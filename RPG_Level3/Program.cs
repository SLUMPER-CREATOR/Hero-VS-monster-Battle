using System;
using System.Collections.Generic;
using System.Linq;

namespace TextRPG
{
    
    public interface IUsable
    {
        string Name { get; }
        void Use(Character target);
    }

    public interface IDamageable
    {
        int Health { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }
        void TakeDamage(int amount, Character? source);
    }
    

    
    public abstract class Character : IDamageable
    {
        public string Name { get; }
        public int MaxHealth { get; protected set; }
        
        private int _health;
        public int Health
        {
            get => _health;
            set => _health = Math.Clamp(value, 0, MaxHealth);
        }

        public int BaseDamage { get; protected set; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel { get; private set; } = 100;

        public bool IsAlive => Health > 0;
        public event Action<Character>? OnDeath;

        public List<Item> Inventory { get; } = new();
        public Item? EquippedItem { get; private set; }

        protected Character(string name, int maxHealth, int baseDamage)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            BaseDamage = baseDamage;
        }

        public virtual void TakeDamage(int amount, Character? source)
        {
            if (!IsAlive) return;

            Health -= amount;
            Console.WriteLine($"{Name} получает {amount} урона. Текущее здоровье: {Health}/{MaxHealth}");

            if (!IsAlive)
            {
                Console.WriteLine($"{Name} побеждён!");
                OnDeath?.Invoke(this);
            }
        }

        public virtual int CalculateDamage() => BaseDamage + (EquippedItem?.DamageBonus ?? 0);

        public abstract void Attack(Character target);

        public void RestoreHealth(int amount) => Health += amount;

        public void GainExperience(int amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNextLevel)
            {
                Level++;
                Experience -= ExperienceToNextLevel;
                ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.5);
                MaxHealth += 10;
                BaseDamage += 2;
                Health = Math.Min(Health + 20, MaxHealth);
                Console.WriteLine($"[LEVEL UP] {Name} получил новый уровень!");
            }
        }

        public void AddItem(Item item) => Inventory.Add(item);
        public void RemoveItem(Item item) => Inventory.Remove(item);
        public void EquipItem(Item item) => EquippedItem = item;

        public void ShowInventory()
        {
            Console.WriteLine($"--- Инвентарь {Name} ---");
            if (Inventory.Count == 0) Console.WriteLine("(Пусто)");
            else
            {
                for (int i = 0; i < Inventory.Count; i++)
                    Console.WriteLine($"  [{i}] {Inventory[i].Name}");
            }
            if (EquippedItem != null) Console.WriteLine($"  [E] Экипировано: {EquippedItem.Name}");
            Console.WriteLine("--------------------------------");
        }

        public void UseItem(int index, Character target)
        {
            if (index < 0 || index >= Inventory.Count)
            {
                Console.WriteLine("Ошибка: неверный индекс предмета.");
                return;
            }

            var item = Inventory[index];
            item.Use(target);
            Inventory.RemoveAt(index);
            Console.WriteLine($"{item.Name} удалён из инвентаря.");
        }
    }
    

    
    public class Warrior : Character
    {
        private readonly Random _rng = new();

        public Warrior(string name, int health, int damage) : base(name, health, damage) { }

        public override void Attack(Character target)
        {
            int damage = CalculateDamage();
            
            if (_rng.NextDouble() < 0.3)
            {
                damage *= 2;
                Console.WriteLine($"{Name} наносит критический удар!");
            }

            Console.WriteLine($"{Name} атакует {target.Name} на {damage} урона.");
            target.TakeDamage(damage, this);
        }
    }

    public class Mage : Character
    {
        private readonly Random _rng = new();

        public Mage(string name, int health, int damage) : base(name, health, damage) { }

        public override void Attack(Character target)
        {
            int damage = CalculateDamage();
            Console.WriteLine($"{Name} кастует заклинание на {target.Name} на {damage} урона.");
            target.TakeDamage(damage, this);

            if (_rng.NextDouble() < 0.2)
            {
                base.RestoreHealth(15);
                Console.WriteLine($"{Name} восстанавливает 15 здоровья магией.");
            }
        }
    }

    public class Monster : Character
    {
        public Monster(string name, int health, int damage) : base(name, health, damage) { }

        public override void Attack(Character target)
        {
            int damage = CalculateDamage();
            Console.WriteLine($"{Name} атакует {target.Name} на {damage} урона.");
            target.TakeDamage(damage, this);
        }
    }
    

    
    public abstract class Item
    {
        public string Name { get; }
        public int DamageBonus { get; protected set; }
        protected Item(string name, int damageBonus = 0) { Name = name; DamageBonus = damageBonus; }
        public abstract void Use(Character target);
    }

    public class HealthPotion : Item
    {
        private readonly int _healAmount;
        public HealthPotion(string name, int healAmount) : base(name) => _healAmount = healAmount;
        public override void Use(Character target)
        {
            target.RestoreHealth(_healAmount);
            Console.WriteLine($"{target.Name} выпивает {Name}. HP восстановлено на {_healAmount}");
        }
    }

    public class Sword : Item
    {
        public Sword(string name, int damageBonus) : base(name, damageBonus) { }
        public override void Use(Character target)
        {
            target.EquipItem(this);
            Console.WriteLine($"{target.Name} экипирует {Name}. Базовый урон +{DamageBonus}");
        }
    }

    public class Bomb : Item
    {
        private readonly int _damage;
        public Bomb(string name, int damage) : base(name) => _damage = damage;
        public override void Use(Character target)
        {
            Console.WriteLine($"{target.Name} бросает {Name}! Взрывная область: {_damage} урона.");
            target.TakeDamage(_damage, null);
        }
    }

    public class ManaPotion : Item
    {
        private readonly int _manaRestore;
        public ManaPotion(string name, int manaRestore) : base(name) => _manaRestore = manaRestore;
        public override void Use(Character target) =>
            Console.WriteLine($"{target.Name} пьёт {Name}. Мана восстановлена на {_manaRestore}");
    }
    
    public static class BattleLogger
    {
        public static void Subscribe(Character character) => character.OnDeath += LogDeath;
        private static void LogDeath(Character character) =>
            Console.WriteLine($"[BATTLE LOG] Персонаж {character.Name} погиб.");
    }
    
    class Program
    {
        static void Main()
        {

            var heroes = new List<Character>
            {
                new Warrior("Артур", 100, 15),
                new Mage("Элара", 80, 20)
            };

            var monsters = new List<Character>
            {
                new Monster("Гоблин-воин", 70, 12),
                new Monster("Орк-берсерк", 120, 18)
            };

            foreach (var c in heroes.Concat(monsters)) BattleLogger.Subscribe(c);

            heroes[0].AddItem(new HealthPotion("Зелье здоровья", 30));
            heroes[0].AddItem(new Sword("Стальной меч", 5));
            heroes[0].AddItem(new Bomb("Взрывчатка", 25));
            heroes[0].AddItem(new ManaPotion("Зелье маны", 20));
            heroes[0].ShowInventory();
            Console.WriteLine();

            int turn = 1;
            while (heroes.Any(h => h.IsAlive) && monsters.Any(m => m.IsAlive))
            {
                Console.WriteLine($"--- Ход {turn} ---");
                
                foreach (var hero in heroes.Where(h => h.IsAlive).ToList())
                {
                    if (!monsters.Any(m => m.IsAlive)) break;
                    var target = monsters.First(m => m.IsAlive);
                    
                    if (hero.Health < hero.MaxHealth * 0.4 && hero.Inventory.Any(i => i is HealthPotion))
                    {
                        int potIndex = hero.Inventory.FindIndex(i => i is HealthPotion);
                        hero.UseItem(potIndex, hero);
                    }
                    
                    hero.Attack(target);
                    if (!target.IsAlive) monsters.Remove(target);
                }

                if (!monsters.Any(m => m.IsAlive)) break;

                foreach (var monster in monsters.Where(m => m.IsAlive).ToList())
                {
                    var target = heroes.First(h => h.IsAlive);
                    monster.Attack(target);
                    if (!target.IsAlive) heroes.Remove(target);
                }

                turn++;
                Console.WriteLine();
            }

            Console.WriteLine("=== Бой окончен ===");
            if (heroes.Any(h => h.IsAlive))
            {
                Console.WriteLine("Победа героев!");
                foreach (var h in heroes.Where(h => h.IsAlive))
                {
                    h.GainExperience(150);
                    h.ShowInventory();
                }
            }
            else
            {
                Console.WriteLine("Монстры победили. Герои пали в бою.");
            }

            Console.WriteLine("\nПрограмма завершена.");
        }
    }
    
}