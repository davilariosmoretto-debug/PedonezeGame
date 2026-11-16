
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

using PhyAttack;
using MagicSystem;

using MainCharacterFile;
using GenericEnemy;

using static EventSystem.EventHandler;
using EventSystem;

namespace CharacterFile
{
    public abstract class Character2D
    {
        private int healthmax;
        private int health;
        private int xp;

        public string Name { get; set; }
        
        public int healthMax {
            get => healthmax;
            protected set => healthmax = Math.Clamp(value, 0, int.MaxValue);
        }

        public int Health {
            get => health;
            set => health = Math.Clamp(value, 0, healthMax);
        }

        public int Xp {
            get => xp;
            set => xp = Math.Clamp(value, 1, 100);
        }
    }

    public abstract partial class Combatant : Character2D
    {

        private const int MAX_ARMOUR = 20;
        private const int MAX_INITIATIVE_BONUS = 20;

        protected Combatant(
                string name,
                int healthMax,
                int armor,
                int initiativeBonus,
                int xp)
        {
            Name = name;
            this.healthMax = healthMax;
            Health = healthMax;
            Armour = armor;
            InitiativeBonus = initiativeBonus;
            Xp = xp;
        }

        public int Id { get; set; }
        public int PastState { get; set; }

        public List<int> StateTurns { get; set; } = new();

        private int armour;
        private int initiativeBonus;

        private int healthCopy;
        private int armourCopy;

        public List<Effect> ActiveEffects { get; set; } = new();
        public List<Resistance> Resistances { get; set; } = new();

        public static List<Combatant> Hero { get; set; } = new();
        public static List<Combatant> Enemy { get; set; } = new();
        public static List<Combatant> All { get; set; } = new();

        public int Armour
        {
            get => armour;
            set => armour = Math.Clamp(value, 0, MAX_ARMOUR);
        }

        public int InitiativeBonus
        {
            get => initiativeBonus;
            set => initiativeBonus = Math.Clamp(value, 0, MAX_INITIATIVE_BONUS);
        }
    }
    // methods/functions
    public abstract partial class Combatant {        
        public bool ValidTurn() => !( ActiveEffects.Any(eff => eff.Kind == EffectKind.Numbness) || Health <= 0);

        private void throwDamage(Attack atk, Combatant comb) {
            if (comb is MainCharacter main) {
                main.Card.UpdateValues(main);
                ThrowEvent(new Event("damage text", EventKind.Label, 
                    EventBody.Label(main.Card.DamageLabel, $"{atk.Damage}")));
                ThrowEvent(new Event("damage animation", EventKind.Animation,
                    EventBody.Animation(main.Card.Anim, "PlayerDamage", 500)));
            }

            else if (comb is Enemy enm) { 
                enm.Card.UpdateValues(enm); 
                ThrowEvent(new Event("damage text", EventKind.Label, 
                    EventBody.Label(enm.Card.DamageLabel, $"{atk.Damage}")));
                ThrowEvent(new Event("damage animation", EventKind.Animation,
                    EventBody.Animation(enm.Card.Anim, "EnemyDamage", 500)));
            }    
        }

        private int processAttack(Attack atk, Combatant comb) {
            atk.ApplyResistance(comb);
            atk.ApplyBoost(this);
            return atk.Damage;
        }

        public void AttackTarget(Attack attack, Combatant target) {
            target.Health -= processAttack(attack, target);
            throwDamage(attack, target);
        }

        public void AttackTarget(Attack attack, Combatant[] group) {
            if (attack.Range == ActRange.Broad) {
                foreach (Combatant target in group) {
                    target.Health -= processAttack(attack, target);
                    throwDamage(attack, target);
                }
            } else {
                throw new ArgumentException($"You must provide a target for an attack of range focused {nameof(attack)} {nameof(attack.Range)}");
            }
        }
    }
}
