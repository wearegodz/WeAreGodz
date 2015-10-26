using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
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


namespace GosuMechanics_Vayne
{
    class Program
    {
        public static Spell.Targeted E;
        public static Spell.Skillshot Condemn;
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Active R;
        public static Spell.Active Heal;
        public static string Author = "WeAreGodz";
        public static string AddonName = "GosuMechanics Vayne";
        public static Menu menu;
        public static Orbwalking.Orbwalker orbwalker;
        private static BuffType[] buffs;
        private static AIHeroClient rengarLeap;
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }
        public static float ManaPercent { get { return myHero.Mana / myHero.MaxMana * 100; } }
        public static float HealthPercent { get { return myHero.Health / myHero.MaxHealth * 100; } }

        static void Main(string[] args)
        {
            if (Hacks.RenderWatermark)
            {
                Hacks.RenderWatermark = false;
            }

            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (myHero.Hero != Champion.Vayne) { return; }
            Chat.Print("<font color=\"#F20000\"><b>GosuMechanics Vayne:</b></font> Loaded!");

            Q = new Spell.Skillshot(SpellSlot.Q, 325, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Active(SpellSlot.R, 1200);
            Condemn = new Spell.Skillshot(SpellSlot.E, 550, SkillShotType.Linear, (int)0.125, 1200);
            var slot = myHero.GetSpellSlotFromName("summonerheal");
            if (slot != SpellSlot.Unknown)
            {
                Heal = new Spell.Active(slot, 600);
            }

            //Fluxy's
            TargetSelector2.init();

            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            menu.AddLabel(AddonName + " made by " + Author);
            menu.AddLabel("If you find some bugs, please do report it at my EloBuddy Add-On Thread.Thank You!");

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("ELast", new CheckBox("Use E Secure Kill", true));
            SubMenu["Combo"].Add("PushDistance", new Slider("E Push Distance", 425, 300, 475));
            SubMenu["Combo"].Add("R", new CheckBox("Use R", true));
            SubMenu["Combo"].Add("R2", new Slider("when >= enemy in range", 2, 0, 5));
            SubMenu["Combo"].Add("noaastealth", new CheckBox("No AA while stealth", true));
            SubMenu["Combo"].Add("noaastealth2", new Slider("when >= enemy in range", 2, 0, 5));
            SubMenu["Combo"].AddSeparator(10);
            SubMenu["Combo"].AddGroupLabel("Item Settings");
            SubMenu["Combo"].Add("useItems", new CheckBox("Use Items", true));
            SubMenu["Combo"].Add("myhp", new Slider("Use BOTRK if my HP < %", 20, 0, 100));
            SubMenu["Combo"].AddSeparator(10);
            SubMenu["Combo"].AddGroupLabel("Semi Manual E Cast");
            SubMenu["Combo"].Add("castE", new KeyBind("Cast E to closest enemy", false, KeyBind.BindTypes.HoldActive, 'E'));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Harass"].Add("E", new CheckBox("Use E", true));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LastHit"].Add("LastHitMana", new Slider("Mana >=", 50, 0, 100));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("LaneClearMana", new Slider("Mana >=", 50, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("JungleClearMana", new Slider("Mana >=", 50, 0, 100));


            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].AddGroupLabel("AntiGapcloser/Interrupt Settings");
            SubMenu["Misc"].Add("UseEInterrupt", new CheckBox("Use E to Interrupt/Antigapcloser", true));
            SubMenu["Misc"].AddSeparator(10);
            foreach (var hero in EntityManager.Heroes.Enemies.Where(x => x.IsEnemy))
            {
                SubMenu["Misc"].Add(hero.ChampionName, new CheckBox("Use Interrupt/Antigapcloser to " + hero.ChampionName, true));
            }
            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Condemn Settings");
            var mode = SubMenu["Misc"].Add("Mode", new Slider("Condemn Method", 1, 0, 2));
            var modeDisplay = new[] { "Method 1", "Method 2", "Method 3" };
            mode.DisplayName = modeDisplay[mode.CurrentValue];

            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = modeDisplay[changeArgs.NewValue];
                };

            foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => x.IsEnemy))
            {
                SubMenu["Misc"].Add(enemy.ChampionName + "E", new CheckBox("Use Condemn if target is " + enemy.ChampionName, true));
            }

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Focus W Target");
            SubMenu["Misc"].Add("focusW", new CheckBox("Focus Target with W Buff to proc passive", true));

            SubMenu["Misc"].Add("DrawTarget", new CheckBox("Draw Target", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Heal Settings");
            SubMenu["Misc"].Add("heal", new CheckBox("Use SummonerHeal", true));
            SubMenu["Misc"].Add("hp", new Slider("HP <=", 20, 0, 100));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].Add("autobuy", new CheckBox("Auto-Buy Trinkets", true));

            SubMenu["QSS"] = menu.AddSubMenu("Auto QSS/Mercurial", "QSS");
            SubMenu["QSS"].AddGroupLabel("Auto QSS/Mercurial Settings");
            SubMenu["QSS"].Add("use", new KeyBind("Use QSS/Mercurial", true, KeyBind.BindTypes.PressToggle, 'K'));
            SubMenu["QSS"].Add("delay", new Slider("Activation Delay", 1000, 0, 2000));

            buffs = new[]
            {
                BuffType.Blind, BuffType.Charm, BuffType.CombatDehancer, BuffType.Fear, BuffType.Flee, BuffType.Knockback,
                BuffType.Knockup, BuffType.Polymorph, BuffType.Silence, BuffType.Sleep, BuffType.Snare, BuffType.Stun,
                BuffType.Suppression, BuffType.Taunt, BuffType.Poison
            };

            for (int i = 0; i < buffs.Length; i++)
            {
                SubMenu["QSS"].Add(buffs[i].ToString(), new CheckBox(buffs[i].ToString(), true));
            }

            orbwalker = new Orbwalking.Orbwalker(menu);

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Player.OnIssueOrder += Player_OnIssueOrder;
        }

        private static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (sender.IsMe
                 && (args.Order == GameObjectOrder.AttackUnit || args.Order == GameObjectOrder.AttackTo)
                 && (SubMenu["Combo"]["noaastealth"].Cast<CheckBox>().CurrentValue && ObjectManager.Player.CountEnemiesInRange(1000f) > SubMenu["Combo"]["noaastealth2"].Cast<Slider>().CurrentValue)
                 && UltActive() || myHero.HasBuffOfType(BuffType.Invisibility)
                 && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                args.Process = false;
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var target = gapcloser.Sender;

            if (!target.IsValidTarget(E.Range))
            {
                return;
            }

            if (E.IsReady() && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && SubMenu["Misc"][target.ChampionName].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(target);
            }
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range) && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && SubMenu["Misc"][sender.ChampionName].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(sender);
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var enemy = TargetSelector2.GetTarget(E.Range, DamageType.Physical);

            if (enemy == null)
            {
                return;
            }

            var mousePos = myHero.Position.Extend(Game.CursorPos, Q.Range);
            if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo  &&
                enemy.IsValidTarget() && Q.IsReady())
            {
                var after = myHero.Position + (Game.CursorPos - myHero.Position).Normalized() * 300;
                var disafter = Vector3.DistanceSquared(after, enemy.Position);
                if ((disafter < 630 * 630) && disafter > 150 * 150)
                {
                    myHero.Spellbook.CastSpell(SpellSlot.Q, mousePos.To3D(), true); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    Console.WriteLine(" Q1");
                }
                if (Vector3.DistanceSquared(enemy.Position, myHero.Position) > 630 * 630 &&
                    disafter < 630 * 630)
                {
                    myHero.Spellbook.CastSpell(SpellSlot.Q, mousePos.To3D(), true); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    Console.WriteLine(" Q2");
                }
                else
                {
                    myHero.Spellbook.CastSpell(SpellSlot.Q, mousePos.To3D(), true); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    Console.WriteLine(" Q3");
                }
            }       
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {

            if (sender.Name == "Rengar_LeapSound.troy" && sender.IsEnemy)
            {
                foreach (AIHeroClient enemy in
                    ObjectManager.Get<AIHeroClient>()
                        .Where(hero => hero.IsValidTarget(1500) && hero.ChampionName == "Rengar"))
                {
                    rengarLeap = enemy;
                }
            }
            if (rengarLeap != null && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && myHero.Distance(rengarLeap, true) < 1000 * 1000)
            {
                CastCondemn();
            }
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            var enemy = TargetSelector2.GetTarget(E.Range, DamageType.Physical);

            if (enemy == null)
            {
                return;
            }

            if (enemy != null)
            {
                var mousePos = myHero.Position.Extend(Game.CursorPos, Q.Range);
                if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                     enemy.IsValidTarget() && Q.IsReady())
                {
                    myHero.Spellbook.CastSpell(SpellSlot.Q, mousePos.To3D(), true); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    orbwalker.ForceTarget(enemy);
                }

                if (SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue && E.IsReady()
                && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                enemy.IsValidTarget())
                {
                    E.Cast(enemy);
                    orbwalker.ForceTarget(enemy);
                }
            }

            var LastHitE = myHero;

            foreach (var Etarget in EntityManager.Heroes.Enemies.Where(Etarget => Etarget.IsValidTarget(E.Range) && Etarget.Path.Count() < 2))
            {
                if (SubMenu["Combo"]["ELast"].Cast<CheckBox>().CurrentValue && E.IsReady() && myHero.CountEnemiesInRange(600) <= 1)
                {
                    var dmgE = myHero.GetSpellDamage(Etarget, SpellSlot.E);
                    if (dmgE > Etarget.Health || (WTarget(Etarget) == 2 && dmgE + Wdmg(Etarget) > Etarget.Health))
                    {
                        LastHitE = Etarget;

                    }
                }

                if (LastHitE != myHero)
                {
                    E.Cast(LastHitE);
                }
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case Orbwalking.OrbwalkingMode.Clear:
                    LaneClear();
                    JungleClear();
                    break;
            }

            if (SubMenu["QSS"]["use"].Cast<KeyBind>().CurrentValue)
            {
                for (int i = 0; i < buffs.Length; i++)
                {
                    if (myHero.HasBuffOfType(buffs[i]) && SubMenu["QSS"][buffs[i].ToString()].Cast<CheckBox>().CurrentValue && myHero.CountEnemiesInRange(800) > 0)
                    {
                        var delay = SubMenu["QSS"]["delay"].Cast<Slider>().CurrentValue;
                        if (Item.CanUseItem(3140))
                        {
                            Core.DelayAction(() => { Item.UseItem(3140); }, delay);
                        }
                        else if (Item.CanUseItem(3139))
                        {
                            Core.DelayAction(() => { Item.UseItem(3139); }, delay);
                        }
                    }
                }
            }

            if (Game.MapId == GameMapId.SummonersRift)
            {
                if (myHero.IsInShopRange() && SubMenu["Misc"]["autobuy"].Cast<CheckBox>().CurrentValue &&
                    myHero.Level > 6 && Item.HasItem((int)ItemId.Warding_Totem_Trinket))
                {
                    Shop.BuyItem(ItemId.Scrying_Orb_Trinket);
                }
                if (myHero.IsInShopRange() && SubMenu["Misc"]["autobuy"].Cast<CheckBox>().CurrentValue &&
                    !Item.HasItem((int)ItemId.Oracles_Lens_Trinket, myHero) && myHero.Level > 6 &&
                    EntityManager.Heroes.Enemies.Any(
                        h =>
                            h.BaseSkinName == "Rengar" || h.BaseSkinName == "Talon" ||
                            h.BaseSkinName == "Vayne"))
                {
                    Shop.BuyItem(ItemId.Sweeping_Lens_Trinket);
                }
                if (myHero.IsInShopRange() && SubMenu["Misc"]["autobuy"].Cast<CheckBox>().CurrentValue &&
                    myHero.Level >= 9 && Item.HasItem((int)ItemId.Sweeping_Lens_Trinket))
                {
                    Shop.BuyItem(ItemId.Oracles_Lens_Trinket);
                }
            }


            if (SubMenu["Misc"]["focusW"].Cast<CheckBox>().CurrentValue)
            {
                if (FocusWTarget == null && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo ||
                    orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed)
                {
                    return;
                }
                if (FocusWTarget.IsValidTarget() && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                    orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    TargetSelector.GetPriority(FocusWTarget);
                    Console.WriteLine("Focus W");
                }
                else
                {
                    TargetSelector.GetPriority(
                        TargetSelector2.GetTarget(myHero.AttackRange, DamageType.Physical));
                }
            }

            AIHeroClient bestEnemy = null;
            foreach (var target in EntityManager.Heroes.Enemies.Where(target => target.IsValidTarget(E.Range)))
            {
                if (target == null)
                {
                    return;
                }

                if (bestEnemy == null)
                    bestEnemy = target;

                else if (myHero.Distance(target.Position) < myHero.Distance(bestEnemy.Position))
                    bestEnemy = target;

                if (SubMenu["Combo"]["castE"].Cast<KeyBind>().CurrentValue && bestEnemy != null)
                {
                    E.Cast(bestEnemy);
                }
            }
        }


        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("vaynetumble"))
            {
                Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
            }

            if (!sender.IsMe) return;
            var mousePos = myHero.Position.Extend(Game.CursorPos, Q.Range);
            if (args.SData.Name.ToLower().Contains("attack"))
            {
                Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
            }

            if (sender is AIHeroClient)
            {
                var pant = (AIHeroClient)sender;
                if (pant.IsValidTarget() && pant.ChampionName == "Pantheon" && pant.GetSpellSlotFromName(args.SData.Name) == SpellSlot.W)
                {
                    if (SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && args.Target.IsMe)
                    {
                        if (pant.IsValidTarget(E.Range))
                        {
                            E.Cast(pant);
                        }
                    }
                }
            }
            if (args.SData.Name.ToLower() == "zedult" && args.Target.IsMe)
            {
                if (Item.CanUseItem(3140))
                {
                    Core.DelayAction(() => { Item.UseItem(3140); }, 1000);
                }
                else if (Item.CanUseItem(3139))
                {
                    Core.DelayAction(() => { Item.UseItem(3139); }, 1000);
                }
            }
        }

        private static void Combo()
        {
            if (Heal != null && SubMenu["Misc"]["heal"].Cast<CheckBox>().CurrentValue && Heal.IsReady() && HealthPercent <= SubMenu["Misc"]["hp"].Cast<Slider>().CurrentValue
                 && myHero.CountEnemiesInRange(600) > 0 && Heal.IsReady())
            {
                Heal.Cast();
                Console.WriteLine("heal ");
            }

            var target = TargetSelector2.GetTarget(E.Range, DamageType.Physical);
            orbwalker.ForceTarget(target);

            if (!target.IsValidTarget())
            {
                return;
            }
            if (SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue && target != null && target.IsValidTarget() && E.IsReady() &&
                SubMenu["Misc"][target.ChampionName + "E"].Cast<CheckBox>().CurrentValue)
            {
                CondemnMode();
                Console.WriteLine(" E");
            }

            if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue && myHero.CountEnemiesInRange(600f) >= (SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue) && R.IsReady())
            {
                R.Cast();
                Console.WriteLine("R");
            }
            var mousePos = myHero.Position.Extend(Game.CursorPos, Q.Range);

            if ((SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue && Q.IsReady() && myHero.HasBuff("vayneinquisition") && myHero.CountEnemiesInRange(1500) > 0 && myHero.CountEnemiesInRange(670) != 1))
            {
                myHero.Spellbook.CastSpell(SpellSlot.Q, mousePos.To3D(), true);
                Console.WriteLine(" RQ");
            }
            if (SubMenu["Combo"]["useItems"].Cast<CheckBox>().CurrentValue)
            {
                if (Item.HasItem((int)ItemId.Blade_of_the_Ruined_King, myHero) && Item.CanUseItem((int)ItemId.Blade_of_the_Ruined_King)
                    && HealthPercent <= SubMenu["Combo"]["myhp"].Cast<Slider>().CurrentValue)
                {
                    Item.UseItem((int)ItemId.Blade_of_the_Ruined_King, target);
                }
                if (Item.HasItem((int)ItemId.Bilgewater_Cutlass, myHero) && Item.CanUseItem((int)ItemId.Bilgewater_Cutlass)
                   && target.IsValidTarget())
                {
                    Item.UseItem((int)ItemId.Bilgewater_Cutlass, target);
                }
                if (Item.HasItem((int)ItemId.Youmuus_Ghostblade, myHero) && Item.CanUseItem((int)ItemId.Youmuus_Ghostblade)
                   && myHero.Distance(target.Position) <= myHero.GetAutoAttackRange())
                {
                    Item.UseItem((int)ItemId.Youmuus_Ghostblade);
                }
            }
        }

        private static void LastHit()
        {
            if (Q.IsReady() && SubMenu["LastHit"]["Q"].Cast<CheckBox>().CurrentValue)
            {
                var Minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, myHero.GetAutoAttackRange(), true);
                foreach (var minions in
                    Minions.Where(
                        minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q)))
                {
                    if (minions != null && minions.IsValidTarget())
                    {
                        Q.Cast(minions);
                        Orbwalker.ForcedTarget = minions;
                        Console.WriteLine("lasthit Q");
                    }
                }
            }
        }

        private static void LaneClear()
        {
            if (Q.IsReady() && SubMenu["LaneClear"]["Q"].Cast<CheckBox>().CurrentValue)
            {
                var Minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position, Q.Range, true);
                foreach (var minions in
                    Minions.Where(
                        minions => minions.Health < ObjectManager.Player.GetSpellDamage(minions, SpellSlot.Q)))
                {
                    if (minions != null && minions.IsValidTarget() && minions.IsVisible)
                    {
                        Q.Cast(minions);
                        Orbwalker.ForcedTarget = minions;
                        Console.WriteLine("laneclear Q");
                    }
                }
            }
        }

        private static void JungleClear()
        {
            Obj_AI_Base jungleMobs = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.Position, Q.Range, true).FirstOrDefault();
            {
                if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue && Q.IsReady() && jungleMobs != null && jungleMobs.IsValidTarget(Q.Range))
                {
                    Q.Cast(Game.CursorPos);
                    Console.WriteLine("jungle Q");
                }
                if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue && E.IsReady() && jungleMobs != null && jungleMobs.IsValidTarget())
                {
                    if (jungleMobs.BaseSkinName == "SRU_Razorbeak" || jungleMobs.BaseSkinName == "SRU_Red" ||
                    jungleMobs.BaseSkinName == "SRU_Blue" ||
                    jungleMobs.BaseSkinName == "SRU_Krug" || jungleMobs.BaseSkinName == "SRU_Gromp" ||
                    jungleMobs.BaseSkinName == "Sru_Crab")
                    {
                        var pushDistance = 425;
                        var targetPosition = Condemn.GetPrediction(jungleMobs).UnitPosition;
                        var pushDirection = (targetPosition - ObjectManager.Player.ServerPosition).Normalized();
                        float checkDistance = pushDistance / 40f;
                        for (int i = 0; i < 40; i++)
                        {
                            Vector3 finalPosition = targetPosition + (pushDirection * checkDistance * i);
                            var collFlags = NavMesh.GetCollisionFlags(finalPosition);
                            if (collFlags.HasFlag(CollisionFlags.Wall) || collFlags.HasFlag(CollisionFlags.Building))
                            {
                                E.Cast(jungleMobs);
                                Orbwalker.ForcedTarget = jungleMobs;
                                Console.WriteLine("jungle E");
                            }
                        }
                    }
                }
            }
        }

        public static bool UltActive()
        {
            return (myHero.HasBuff("vaynetumblefade") && !UnderTower(myHero.Position));
        }

        public static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + ObjectManager.Player.BoundingRadius);
        }

        public static AIHeroClient FocusWTarget
        {
            get
            {
                return ObjectManager.Get<AIHeroClient>().Where(enemy => !enemy.IsDead && enemy.IsValidTarget((Q.IsReady() ? Q.Range : 0) + myHero.AttackRange + 300))
                       .FirstOrDefault(
                           enemy => enemy.Buffs.Any(buff => buff.Name == "vaynesilvereddebuff" && buff.Count > 0));
            }
        }

        public static float Wdmg(Obj_AI_Base target)
        {
            var dmg = (W.Level * 10 + 10) + ((0.03 + (W.Level * 0.01)) * target.MaxHealth);
            return (float)dmg;

        }

        public static int WTarget(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "vaynesilvereddebuff")
                    return buff.Count;
            }
            return -1;
        }

        private static void CastCondemn()
        {
            if (rengarLeap.ChampionName == "Rengar")
            {
                if (rengarLeap.IsValidTarget(E.Range) && E.IsReady() &&
                    rengarLeap.Distance(myHero) <= E.Range)
                {
                    E.Cast(rengarLeap);
                    Console.WriteLine("E Rengar");
                }
            }
        }

        private static void CondemnMode()
        {
            var Mode = SubMenu["Misc"]["Mode"].DisplayName;

            switch (Mode)
            {
                case "Method 1":
                    foreach (var target in EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(E.Range)))
                    {
                        var pushDistance = SubMenu["Combo"]["PushDistance"].Cast<Slider>().CurrentValue;
                        var targetPosition = Condemn.GetPrediction(target).UnitPosition;
                        var pushDirection = (targetPosition - ObjectManager.Player.ServerPosition).Normalized();
                        float checkDistance = pushDistance / 40f;
                        for (int i = 0; i < 40; i++)
                        {
                            Vector3 finalPosition = targetPosition + (pushDirection * checkDistance * i);
                            var collFlags = NavMesh.GetCollisionFlags(finalPosition);
                            if (collFlags.HasFlag(CollisionFlags.Wall) || collFlags.HasFlag(CollisionFlags.Building))
                            {
                                E.Cast(target);
                            }
                        }
                    }
                    break;
                case "Method 2":
                    if (!E.IsReady()) return;
                    if (((orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue)))
                    {
                        foreach (var hero in from hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsValidTarget(550f)) let prediction = Condemn.GetPrediction(hero)
                        where NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(),
                        -SubMenu["Combo"]["PushDistance"].Cast<Slider>().CurrentValue).To3D()).HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(),
                        -(SubMenu["Combo"]["PushDistance"].Cast<Slider>().CurrentValue / 2)).To3D()).HasFlag(CollisionFlags.Wall) select hero)
                        {
                            E.Cast(hero);
                        }
                    }
                    break;
                case "Method 3":
                    foreach (var hero in EntityManager.Heroes.Enemies.Where(hero => hero.IsValidTarget(E.Range) && !hero.HasBuffOfType(BuffType.SpellShield) && !hero.HasBuffOfType(BuffType.SpellImmunity)))
                    {
                        var EPred = Condemn.GetPrediction(hero);
                        int pushDist = SubMenu["Combo"]["PushDistance"].Cast<Slider>().CurrentValue;
                        var FinalPosition = EPred.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -pushDist).To3D();

                        for (int i = 1; i < pushDist; i += (int)hero.BoundingRadius)
                        {
                            Vector3 loc3 = EPred.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -i).To3D();

                            if (loc3.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) || loc3.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building))
                                E.Cast(hero);
                        }
                    }
                    break;
            }
        }
    }
}