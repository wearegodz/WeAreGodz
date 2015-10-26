using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace GosuMechanics_Reksai
{
    class Program
    {
        public static Spell.Active Q;
        public static Spell.Skillshot Q2;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Targeted E2;
        public static string Author = "WeAreGodz";
        public static string AddonName = "GosuMechanics Reksai";
        public static Menu menu;
        private static SpellSlot _smiteSlot = SpellSlot.Unknown;
        private static Spell.Targeted _smite;
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3724, 3723, 3933 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719, 3932 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714, 3931 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707, 3930 };

        static void Main(string[] args)
        {
            if (Hacks.RenderWatermark)
                Hacks.RenderWatermark = false;

            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (myHero.Hero != Champion.RekSai) { return; }
            Chat.Print("<font color=\"#F20000\"><b>GosuMechanics Reksai:</b></font> Loaded!");

            Q = new Spell.Active(SpellSlot.Q);
            Q2 = new Spell.Skillshot(SpellSlot.Q, 1450, SkillShotType.Linear, int.MaxValue, int.MaxValue);
            Q2.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 250);
            E2 = new Spell.Targeted(SpellSlot.E, 500);
            SetSmiteSlot();

            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            menu.AddLabel(AddonName + " made by " + Author);
            menu.AddLabel("If you find some bugs, please do report it at my EloBuddy Add-On Thread.Thank You!");

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("Q2", new CheckBox("Use Q2", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("E2", new CheckBox("Use E2", true));
            SubMenu["Combo"].Add("useitems", new CheckBox("Use Items", true));
            SubMenu["Combo"].Add("usesmite", new CheckBox("Use Smite", true));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Harass"].Add("E", new CheckBox("Use E", true));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].Add("LC", new KeyBind("LaneClear Key", false, KeyBind.BindTypes.HoldActive, 'X'));
            SubMenu["LaneClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["LaneClear"].Add("useitems", new CheckBox("Use Items", true));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("JC", new KeyBind("JungleClear Key", false, KeyBind.BindTypes.HoldActive, 'X'));
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("useitems", new CheckBox("Use Items", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].AddGroupLabel("Auto Q2 Settings");
            SubMenu["Misc"].Add("AutoQ", new KeyBind("Auto Q2", true, KeyBind.BindTypes.PressToggle, 'L'));
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Auto W Settings");
            SubMenu["Misc"].Add("AutoWHP", new Slider("Use W if HP is <= ", 25, 1, 100));
            SubMenu["Misc"].Add("AutoWMP", new Slider("Use W if Fury is >= ", 100, 1, 100));
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].Add("AutoB", new KeyBind("Auto Burrowed", true, KeyBind.BindTypes.PressToggle , 'L'));
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Escape Settings");
            SubMenu["Misc"].Add("EscW", new CheckBox("Use Q", true));
            SubMenu["Misc"].Add("EscE2", new CheckBox("Use E", true));
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("KillSteal Settings");
            SubMenu["Misc"].Add("KsQ", new CheckBox("Use Q", true));
            SubMenu["Misc"].Add("KsE", new CheckBox("Use E", true));
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Smite Settings");
            SubMenu["Misc"].Add("AutoSmite", new KeyBind("Auto Smite Toggle", true, KeyBind.BindTypes.PressToggle, 'L'));
            SubMenu["Misc"].Add("usesmiteRed", new CheckBox("Use Smite Red Instantly", true));
            SubMenu["Misc"].Add("usesmiteRedEarly", new Slider("if myHero HP% <=", 35, 1, 100));

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("reksaiq"))
                Orbwalker.ResetAutoAttack();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            KillSteal();

            if (SubMenu["Misc"]["AutoB"].Cast<KeyBind>().CurrentValue)
            {
                AutoW();
            }
            if (!burrowed(myHero) && SubMenu["Misc"]["AutoB"].Cast<KeyBind>().CurrentValue)
            {
                AutoBurrowed();
            }
            if (SubMenu["Misc"]["AutoSmite"].Cast<KeyBind>().CurrentValue)
            {
                UseSmite();
            }

            if (SubMenu["LaneClear"]["LC"].Cast<KeyBind>().CurrentValue)
            {
                LaneClear();
            }

            if (SubMenu["JungleClear"]["JC"].Cast<KeyBind>().CurrentValue)
            {
                JungleClear();
            }

            if (SubMenu["Misc"]["AutoQ"].Cast<KeyBind>().CurrentValue)
            {
                if (burrowed(myHero) && Q2.IsReady() && !myHero.IsRecalling())
                {
                    foreach (var target in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q2.Range) && myHero.Distance(x) >= Q2.Range && !x.IsZombie))
                    {
                        Q2.Cast(target);
                    }
                }
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Flee:
                    Escape();
                    break;
            }
        }

        private static void AutoW()
        {
            var reksaiHp = (myHero.MaxHealth * (SubMenu["Misc"]["AutoWHP"].Cast<Slider>().CurrentValue) / 100);
            var reksaiMp = (myHero.MaxMana * (SubMenu["Misc"]["AutoWMP"].Cast<Slider>().CurrentValue) / 100);
            if (myHero.IsRecalling()) return;
            if (W.IsReady()&& myHero.Health <= reksaiHp && !burrowed(myHero) && myHero.Mana >= reksaiMp && myHero.CountEnemiesInRange(300) == 0)
            {
                W.Cast();
            }

        }

        private static void AutoBurrowed()
        {
            if (burrowed(myHero) || myHero.IsRecalling() || myHero.IsDead) return;
            if (!burrowed(myHero) && W.IsReady() && myHero.CountEnemiesInRange(300) == 0)
            {
                W.Cast();
            }

        }

        private static bool burrowed(AIHeroClient player)
        {
            return myHero.Spellbook.GetSpell(SpellSlot.Q).Name == "reksaiqburrowed";
        }

        private static double GetRawEDamage()
        {
            var damage = new double[] { 0.8f, 0.9f, 1, 1.1f, 1.2f }[E.Level - 1] * myHero.TotalAttackDamage;
            return damage * (1 + (myHero.Mana / myHero.MaxMana));
        }

        private static void Combo()
        {
            var useQ = SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue;
            var useE = SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue;
            var autoSwitch = SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue;
            var useQ2 = SubMenu["Combo"]["Q2"].Cast<CheckBox>().CurrentValue;
            var useE2 = SubMenu["Combo"]["E2"].Cast<CheckBox>().CurrentValue;
            var useItems = SubMenu["Combo"]["useitems"].Cast<CheckBox>().CurrentValue;
            var useSmite = SubMenu["Combo"]["usesmite"].Cast<CheckBox>().CurrentValue;

            SmiteTarget();

            if (burrowed(myHero))
            {
                if (Q2.IsReady() && useQ2)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie && x.IsValidTarget(Q2.Range)))
                    {
                        PredictionResult QPred = Q2.GetPrediction(enemy);
                        if (myHero.Distance(enemy) > myHero.AttackRange)
                        {
                            Q2.Cast(QPred.CastPosition);
                        }
                    }
                }
                if (E2.IsReady() && useE2)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie && x.IsValidTarget(E2.Range)))
                    {
                        if (myHero.Distance(enemy) >= 200)
                        {
                            E2.Cast(enemy.Position - 50);
                        }
                        if (myHero.Distance(enemy) < myHero.AttackRange && W.IsReady() && !Q2.IsReady() && !E2.IsReady() && autoSwitch) // Auto Switch
                        {
                            W.Cast();
                        }
                    }
                }
            }
            if (!burrowed(myHero))
            {
                if (Q.IsReady() && useQ)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie))
                    {
                        if (myHero.Distance(enemy.Position) < E.Range)
                        {
                            Q.Cast();
                        }
                        if (useItems)
                        {
                            UseItems(enemy);
                        }
                    }
                }
                if (E.IsReady() && useE)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie && x.IsValidTarget(E.Range)))
                    {
                        E.Cast(enemy);

                        if (myHero.Distance(enemy) > myHero.AttackRange && W.IsReady() && autoSwitch) // Auto Switch
                        {
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            var useQ = SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue;
            var useE = SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue;

            if (burrowed(myHero))
            {
                if (Q2.IsReady() && useQ)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie && x.IsValidTarget(Q2.Range)))
                    {
                        if (Q2.GetPrediction(enemy).HitChance >= HitChance.High)
                        {
                            Q2.Cast(enemy.Position);
                        }
                    }
                }
            }
            if (!burrowed(myHero))
            {
                if (E.IsReady() && useE)
                {
                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsZombie && x.IsValidTarget(E.Range)))
                    {
                        E.Cast(enemy);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var useItems = SubMenu["LaneClear"]["useitems"].Cast<CheckBox>().CurrentValue;

            foreach (var minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myHero.Position, Program.Q.Range))
            {
                if (Program.SubMenu["LaneClear"]["Q"].Cast<CheckBox>().CurrentValue && Program.Q.IsReady() && minion.IsValidTarget())
                {
                    if (burrowed(myHero) && Program.Q2.IsReady() && Q2.GetPrediction(minion).HitChance >= HitChance.High)
                    {
                        Program.Q2.Cast(minion);
                    }
                    if (Program.Q.IsReady() && !burrowed(myHero))
                    {
                        Program.Q.Cast();
                    }
                }
                if (Program.SubMenu["LaneClear"]["E"].Cast<CheckBox>().CurrentValue && Program.E.IsReady() && minion.IsValidTarget(Program.E.Range))
                {
                    if (Program.myHero.GetSpellDamage(minion, SpellSlot.E) >= minion.Health)
                    {
                        Program.E.Cast(minion);
                    }
                    else if (Program.E.IsReady())
                    {
                        Program.E.Cast(minion);
                    }
                }
                if (useItems)
                {
                    UseItems(minion);
                }
            }
        }

        private static void JungleClear()
        {
            Obj_AI_Base jungle = EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myHero.Position, Program.Q.Range, true).FirstOrDefault();
            {
                var useQ = Program.SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue;
                var useW = Program.SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue;
                var useE = Program.SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue;
                var useItems = SubMenu["JungleClear"]["useitems"].Cast<CheckBox>().CurrentValue;
                var reksaifury = Equals(myHero.Mana, myHero.MaxMana);


                if (jungle != null && jungle.IsValidTarget())
                {
                    if (useQ && burrowed(myHero) && Q2.IsReady() && Q2.GetPrediction(jungle).HitChance >= HitChance.High)
                    {
                        Q2.Cast(jungle.Position);
                    }
                    if (useQ && !burrowed(myHero) && Q.IsReady())
                    {
                        Q.Cast();
                    }
                    if (useW && !(jungle as Obj_AI_Base).HasBuff("reksaiknockupimmune") && W.IsReady() && !Q.IsReady() &&
                        !E.IsReady() &&
                        jungle.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }
                    if (!burrowed(myHero) && E.IsReady() && useE && myHero.Distance(jungle) < E.Range)
                    {
                        if (reksaifury)
                        {
                            E.Cast(jungle);
                        }
                        else if (jungle.Health <= myHero.GetSpellDamage(jungle, SpellSlot.E))
                        {
                            E.Cast(jungle);
                        }
                        else
                        {
                            E.Cast(jungle);
                        }
                    }
                    if (myHero.Distance(jungle) <= myHero.AttackRange)
                    {
                        W.Cast();
                    }
                    if (useItems)
                    {
                        UseItems(jungle);
                    }
                }
            }
        }

        private static void Escape()
        {
            var mousePos = myHero.Position.Extend(Game.CursorPos, E2.Range);
            if (burrowed(myHero) && E2.IsReady() && SubMenu["Misc"]["EscE2"].Cast<CheckBox>().CurrentValue)
            {
                myHero.Spellbook.CastSpell(SpellSlot.E, mousePos.To3D(), true);
            }
            else if (!burrowed(myHero) && E2.IsReady() && W.IsReady() && SubMenu["Misc"]["EscE2"].Cast<CheckBox>().CurrentValue && SubMenu["Misc"]["EscW"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }
        }

        private static void KillSteal()
        {
            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsEnemy))
            {
                if (SubMenu["Misc"]["KsQ"].Cast<CheckBox>().CurrentValue)
                {
                    if (Q2.IsReady() && hero.IsValidTarget(Q2.Range) && burrowed(myHero))
                    {
                        if (hero.Health <= myHero.GetSpellDamage(hero, SpellSlot.Q, DamageLibrary.SpellStages.SecondCast))
                            Q2.Cast(hero.Position);
                    }
                    if (Q2.IsReady() && W.IsReady() && !hero.IsValidTarget(Q.Range) && hero.IsValidTarget(Q2.Range) &&
                        hero.Health <= myHero.GetSpellDamage(hero, SpellSlot.Q, DamageLibrary.SpellStages.SecondCast))
                    {
                        W.Cast();
                        Q2.Cast(hero.Position);
                    }
                }
                if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) && burrowed(myHero) && hero.IsValidTarget(Q2.Range) && Q2.IsReady() && SubMenu["Misc"]["KsQ"].Cast<CheckBox>().CurrentValue && !myHero.IsRecalling())
                {
                    {
                        Q2.Cast(hero);
                    }
                }
                if (E.IsReady() && hero.IsValidTarget(E.Range) && SubMenu["Misc"]["KsE"].Cast<CheckBox>().CurrentValue &&
                    !burrowed(myHero))
                {
                    if (myHero.Mana <= 100 && hero.Health <= myHero.GetSpellDamage(hero, SpellSlot.E, DamageLibrary.SpellStages.Default))
                    {
                        E.Cast(hero);
                    }
                    if (myHero.Mana == 100 && hero.Health <= myHero.GetSpellDamage(hero, SpellSlot.E, DamageLibrary.SpellStages.Default))
                    {
                        E.Cast(hero);
                    }
                }

                int smiteDmg = GetSmiteDmg();

                if (smiteDmg >= hero.Health)
                {
                    _smite.Cast(hero);
                }
            }
        }

        public static void UseItems(Obj_AI_Base unit)
        {
            if (unit == null)
                return;

            InventorySlot[] items = myHero.InventoryItems;

            foreach (InventorySlot item in items)
            {
                if (item.CanUseItem())
                {
                    if ((item.Id == ItemId.Blade_of_the_Ruined_King || item.Id == ItemId.Bilgewater_Cutlass)
                        && myHero.Distance(unit) <= 550
                        && unit.Type == GameObjectType.AIHeroClient
                        && unit.IsEnemy)
                        item.Cast(unit);

                    if ((item.Id == ItemId.Ravenous_Hydra_Melee_Only || item.Id == ItemId.Tiamat_Melee_Only)
                        && (unit.Type == GameObjectType.AIHeroClient || unit.Type == GameObjectType.obj_AI_Minion)
                        && myHero.Distance(unit) <= 400
                        && unit.IsEnemy)
                        item.Cast();

                    if (item.Id == ItemId.Youmuus_Ghostblade
                        && unit.Type == GameObjectType.AIHeroClient
                        && unit.IsEnemy)
                        item.Cast();
                    if (item.Id == ItemId.Randuins_Omen
                        && unit.Type == GameObjectType.AIHeroClient
                        && myHero.Distance(unit) <= 400
                        && unit.IsEnemy)
                        item.Cast();
                }
            }
        }

        private static string Smitetype()
        {
            if (SmiteBlue.Any(i => Item.HasItem(i)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(i => Item.HasItem(i)))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(i => Item.HasItem(i)))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(i => Item.HasItem(i)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, Smitetype(), StringComparison.CurrentCultureIgnoreCase)))
            {
                _smiteSlot = spell.Slot;
                _smite = new Spell.Targeted(_smiteSlot, 700);
                return;
            }
        }

        private static int GetSmiteDmg()
        {
            int level = myHero.Level;
            int index = myHero.Level / 5;
            float[] dmgs = { 370 + 20 * level, 330 + 30 * level, 240 + 40 * level, 100 + 50 * level };
            return (int)dmgs[index];
        }

        private static void UseSmite()
        {
            if (!_smite.IsReady()) return;

            string[] jungleMinions;
            if (Game.MapId == GameMapId.TwistedTreeline)
            {
                jungleMinions = new string[] { "TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf" };
            }
            else
            {
                jungleMinions = new string[]
                {
                    "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "SRU_Dragon",
                    "SRU_Baron"
                };
            }

            var jungle = Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
            var usered = SubMenu["Misc"]["usesmiteRed"].Cast<CheckBox>().CurrentValue;
            var health = (100 * (myHero.Health / myHero.MaxHealth)) <= SubMenu["Misc"]["usesmiteRedEarly"].Cast<Slider>().CurrentValue;
          
            var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, 1000, true);
            if (minions.Count() > 0)
            {
                int smiteDmg = GetSmiteDmg();

                foreach (Obj_AI_Base minion in minions)
                {
                    if (Game.MapId == GameMapId.TwistedTreeline &&
                        minion.Health <= smiteDmg &&
                        jungleMinions.Any(name => minion.Name.Substring(0, minion.Name.Length - 5).Equals(name)))
                    {
                        _smite.Cast(minion);
                    }
                    if (minion.Health <= smiteDmg && jungleMinions.Any(name => minion.Name.StartsWith(name)) &&
                        !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        _smite.Cast(minion);
                    }
                    else if (jungle && usered && health && minion.Health >= smiteDmg &&
                             jungleMinions.Any(name => minion.Name.StartsWith("SRU_Red")) &&
                             !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        _smite.Cast(minion);
                    }
                }
            }
        }

        private static void SmiteTarget()
        {
            if (_smite != null && _smite.Name.ToLower() == "s5_summonersmiteduel" || _smite.Name.ToLower() == "s5_summonersmiteplayerganker")
            {
                foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsEnemy))
                {
                    int smiteDmg = GetSmiteDmg();
                    var usesmite = SubMenu["Combo"]["usesmite"].Cast<CheckBox>().CurrentValue;
                    if (SmiteBlue.Any(i => Item.HasItem(i)) && usesmite &&
                        _smite.IsReady() && hero.IsValidTarget(_smite.Range))
                    {
                        if (!hero.HasBuffOfType(BuffType.Stun) || !hero.HasBuffOfType(BuffType.Slow))
                        {
                            _smite.Cast(hero);
                        }
                        else if (smiteDmg >= hero.Health)
                        {
                            _smite.Cast(hero);
                        }
                    }
                    if (SmiteRed.Any(i => Item.HasItem(i)) && usesmite &&
                       _smite.IsReady() &&
                        hero.IsValidTarget(_smite.Range))
                    {
                        _smite.Cast(hero);
                    }
                }
            } 
        }
    }
}
