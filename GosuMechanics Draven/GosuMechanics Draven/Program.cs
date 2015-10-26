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

namespace GosuMechanics_Draven
{
    class Program
    {
        public static string Author = "WeAreGodz";
        public static string AddonName = "GosuMechanics Draven";
        public static Menu menu;

        public static Orbwalking.Orbwalker orbwalker;
        public static Spell.Active Q { get; set; }
        public static Spell.Skillshot E { get; set; }
        public static Spell.Skillshot R { get; set; }
        public static Spell.Active W { get; set; }
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }
        public static float ManaPercent {   get {   return myHero.Mana / myHero.MaxMana * 100;}   }

        public static float HealthPercent { get { return myHero.Health / myHero.MaxHealth * 100; } }
        public static Menu Menu { get; set; }
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        public static int QCount   {get {return (myHero.HasBuff("dravenspinning") ? 1 : 0)+ (myHero.HasBuff("dravenspinningleft") ? 1 : 0) + QReticles.Count;} }
        public static List<QRecticle> QReticles { get; set; }
        public static int LastAxeMoveTime { get; set; }

        public static void Main(string[] args)
        {
            if (Hacks.RenderWatermark)
                Hacks.RenderWatermark = false;

            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Q = new Spell.Active(SpellSlot.Q, (uint)myHero.GetAutoAttackRange());
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1100, SkillShotType.Linear, 250, 1400, 130);
            R = new Spell.Skillshot(SpellSlot.R, 20000, SkillShotType.Linear, 400, 2000, 160);

            QReticles = new List<QRecticle>();

            if (myHero.Hero != Champion.Draven) { return; }
            Chat.Print("<font color=\"#F20000\"><b>GosuMechanics Draven:</b></font> Loaded!");

            menu = EloBuddy.SDK.Menu.MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            menu.AddLabel(AddonName + " made by " + Author);
            menu.AddLabel("If you find some bugs, please do report it at my EloBuddy Add-On Thread.Thank You!");

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W", true));
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("R", new CheckBox("Use R", true));
            SubMenu["Combo"].AddSeparator(10);
            SubMenu["Combo"].AddGroupLabel("Item Settings");
            SubMenu["Combo"].Add("Items", new CheckBox("Use Items", true));
            SubMenu["Combo"].Add("myhp", new Slider("Use BOTRK if my HP < %", 20, 0, 100));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("E", new CheckBox("Use E", true));

            SubMenu["Clear"] = menu.AddSubMenu("Clear", "Clear");
            SubMenu["Clear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Clear"].Add("WaveClearManaPercent", new Slider("Mana Percent", 50, 0, 100));

            SubMenu["Misc"] = menu.AddSubMenu("Misc Settings", "Misc");
            SubMenu["Misc"].AddGroupLabel("AntiGapcloser/Interrupt Settings");
            SubMenu["Misc"].Add("UseEInterrupt", new CheckBox("Use E to Interrupt/Antigapcloser", true));
            SubMenu["Misc"].AddSeparator(10);
            foreach (var hero in EntityManager.Heroes.Enemies.Where(x => x.IsEnemy))
            {
                SubMenu["Misc"].Add(hero.ChampionName, new CheckBox("Use Interrupt/Antigapcloser to " + hero.ChampionName, true));
            }
            SubMenu["Misc"].Add("UseWSetting", new CheckBox("Use W Instantly(When Available)", true));
            SubMenu["Misc"].Add("UseWSlow", new CheckBox("Use W when slowed", true));
            SubMenu["Misc"].Add("UseWManaPercent", new Slider("Use W Mana Percent", 50, 0, 100));

            SubMenu["Misc"].AddGroupLabel("Axe Settings");
            var axe1 = SubMenu["Misc"].Add("AxeMode", new Slider("Catch Axe on Mode:", 2, 0, 2));
            var axe2 = new[] { "Combo", "Any", "Always" };
            axe1.DisplayName = axe2[axe1.CurrentValue];

            axe1.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = axe2[changeArgs.NewValue];
                };
            SubMenu["Misc"].Add("CatchAxeRange", new Slider("Catch Axe Range", 800, 120, 1500));
            SubMenu["Misc"].Add("MaxAxes", new Slider("Maximum Axes", 2, 1, 3));
            SubMenu["Misc"].Add("UseWForQ", new CheckBox("Use W if Axe too far", true));
            SubMenu["Misc"].Add("DontCatchUnderTurret", new CheckBox("Don't Catch Axe Under Turret", true));

            SubMenu["Misc"].AddGroupLabel("KS Settings");
            SubMenu["Misc"].Add("KsE", new CheckBox("Use E", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Skin Setting");
            var skin = SubMenu["Misc"].Add("sID", new Slider("Skin", 4, 0, 5));
            var sID = new[] { "Skin1", "Skin2", "Skin3", "Skin4", "Skin5", "Skin6" };
            skin.DisplayName = sID[skin.CurrentValue];

            skin.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = sID[changeArgs.NewValue];
                };

            SubMenu["Draw"] = menu.AddSubMenu("Draw Settings", "Draw");
            SubMenu["Draw"].Add("DrawE", new CheckBox("Draw E", true));
            SubMenu["Draw"].Add("DrawAxeLocation", new CheckBox("Draw Axe Location", true));
            SubMenu["Draw"].Add("DrawAxeRange", new CheckBox("Draw Axe Catch Range", true));
            SubMenu["Draw"].Add("DrawTarget", new CheckBox("Draw Target", true));

            orbwalker = new Orbwalking.Orbwalker(menu);

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            Obj_AI_Base.OnDelete += Obj_AI_Base_OnDelete;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range) && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && SubMenu["Misc"][sender.ChampionName].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(sender);
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

        public static void Clear()
        {
            if (ManaPercent < SubMenu["Clear"]["WaveClearManaPercent"].Cast<Slider>().CurrentValue)
            {
                return;
            }


            if (SubMenu["Clear"]["Q"].Cast<CheckBox>().CurrentValue && QCount < SubMenu["Misc"]["MaxAxes"].Cast<Slider>().CurrentValue - 1 && Q.IsReady()
                && orbwalker.GetTarget() is Obj_AI_Minion && !myHero.Spellbook.IsAutoAttacking)
            {
                Q.Cast();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawE = SubMenu["Draw"]["DrawE"].Cast<CheckBox>().CurrentValue;
            var drawAxeLocation = SubMenu["Draw"]["DrawAxeLocation"].Cast<CheckBox>().CurrentValue;
            var drawAxeRange = SubMenu["Draw"]["DrawAxeRange"].Cast<CheckBox>().CurrentValue;

            if (drawE && E.IsReady())
            {
                Circle.Draw(SharpDX.Color.LightGreen, E.Range, myHero.Position);
            }

            if (drawAxeLocation)
            {
                var bestAxe =
                    QReticles.Where(
                        x =>
                        x.Position.Distance(Game.CursorPos) < SubMenu["Misc"]["CatchAxeRange"].Cast<Slider>().CurrentValue)
                        .OrderBy(x => x.Position.Distance(myHero.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (bestAxe != null)
                {
                    Circle.Draw(SharpDX.Color.Red, 120, bestAxe.Position);
                }

                foreach (var axe in
                    QReticles.Where(x => x.Object.NetworkId != (bestAxe == null ? 0 : bestAxe.Object.NetworkId)))
                {
                    Circle.Draw(SharpDX.Color.LightGreen, 120, axe.Position);
                }
            }

            if (drawAxeRange)
            {
                Circle.Draw(SharpDX.Color.LightGreen, SubMenu["Misc"]["CatchAxeRange"].Cast<Slider>().CurrentValue, Game.CursorPos);
            }
        }

        private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            CatchAxe();
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            QReticles.RemoveAll(x => x.Object.IsDead);

            CatchAxe();
            sChoose();
            KillSteal();

            if (W.IsReady() && SubMenu["Misc"]["UseWSlow"].Cast<CheckBox>().CurrentValue && myHero.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }

            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Clear:
                    Clear();
                    break;
            }
        }

        public static void CatchAxe()
        {
            var catchOption = SubMenu["Misc"]["AxeMode"].Cast<Slider>().CurrentValue;

            if (((catchOption == 0 && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                 || (catchOption == 1 && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None))
                || catchOption == 2)
            {
                var bestReticle =
                    QReticles.Where(
                        x =>
                        x.Object.Position.Distance(Game.CursorPos)
                        < SubMenu["Misc"]["CatchAxeRange"].Cast<Slider>().CurrentValue)
                        .OrderBy(x => x.Position.Distance(myHero.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .ThenBy(x => x.ExpireTime)
                        .FirstOrDefault();
                
                if (Orbwalker.CanMove && bestReticle != null && bestReticle.Object.Position.Distance(myHero.ServerPosition) > 100)
                {
                    var eta = 1000 * (myHero.Distance(bestReticle.Position) / myHero.MoveSpeed);
                    var expireTime = bestReticle.ExpireTime - Utils.GameTimeTickCount;

                    if (eta >= expireTime && SubMenu["Misc"]["UseWForQ"].Cast<CheckBox>().CurrentValue)
                    {
                        W.Cast();
                    }

                    if (SubMenu["Misc"]["DontCatchUnderTurret"].Cast<CheckBox>().CurrentValue)
                    {
                        if (!UnderTower(myHero.ServerPosition) && UnderTower(bestReticle.Object.Position))
                        {
                            orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                        }
                        
                        else if (UnderTower(myHero.ServerPosition) && !UnderTower(bestReticle.Object.Position))
                        {
                            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                            }
                        }
                        else if (!UnderTower(bestReticle.Position))
                        {
                            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                            }
                        }
                    }
                    else
                    {
                        if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                        }
                        else
                        {
                            orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                        }
                    }
                }
                else
                {
                    orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                }
            }
            else
            {
                orbwalker.SetOrbwalkingPoint(Game.CursorPos);
            }
        }

        public static void Combo()
        {
            var target = TargetSelector2.GetTarget(E.Range, DamageType.Physical);
            orbwalker.ForceTarget(target);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue;
            var useW = SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue;
            var useE = SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue;
            var useR = SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue;
            var useItems = SubMenu["Combo"]["Items"].Cast<CheckBox>().CurrentValue;

            if (useItems)
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
            if (useQ && QCount < SubMenu["Misc"]["MaxAxes"].Cast<Slider>().CurrentValue - 1 && Q.IsReady()
                && myHero.IsInAutoAttackRange(target) && !myHero.Spellbook.IsAutoAttacking)
            {
                Q.Cast();
            }

            if (useW && W.IsReady()
                && ManaPercent > SubMenu["Misc"]["UseWManaPercent"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["Misc"]["UseWSetting"].Cast<CheckBox>().CurrentValue)
                {
                    W.Cast();
                }
                else
                {
                    if (!myHero.HasBuff("dravenfurybuff"))
                    {
                        W.Cast();
                    }
                }
            }

            if (useE && E.IsReady() && IsFleeing(target))
            {
                E.Cast(target);
            }
            if (useE && E.IsReady() && !IsFleeing(target))
            {
                foreach (var pos in from enemy in ObjectManager.Get<AIHeroClient>()
                                    where
                                        enemy.IsValidTarget() &&
                                        enemy.Distance(ObjectManager.Player) <=
                                        enemy.BoundingRadius + enemy.AttackRange + ObjectManager.Player.BoundingRadius &&
                                        enemy.IsMelee
                                    let direction =
                                        (enemy.ServerPosition.To2D() - ObjectManager.Player.ServerPosition.To2D()).Normalized()
                                    let pos = ObjectManager.Player.ServerPosition.To2D()
                                    select pos + Math.Min(200, Math.Max(50, enemy.Distance(ObjectManager.Player) / 2)) * direction)
                {
                    E.Cast(pos.To3D());
                }
            }
            if (!useR || !R.IsReady())
            {
                return;
            }

            var killableTarget =
                EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(2000))
                    .FirstOrDefault(
                        x =>
                        myHero.GetSpellDamage(x, SpellSlot.R) * 2 > x.Health
                        && (!myHero.IsInAutoAttackRange(x) || myHero.CountEnemiesInRange(E.Range) > 2));

            if (killableTarget != null)
            {
                R.Cast(killableTarget);
            }
        }

        public static bool IsFleeing(AIHeroClient hero)
        {
            var position = E.GetPrediction(hero);
            return position != null &&
                   Vector3.DistanceSquared(ObjectManager.Player.Position, position.CastPosition) >
                   Vector3.DistanceSquared(hero.Position, position.CastPosition);
        }

        public static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                E.Cast(target);
            }
        }

        public static void KillSteal()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget(E.Range))
                {
                    if (SubMenu["Misc"]["KsE"].Cast<CheckBox>().CurrentValue && (myHero.GetSpellDamage(enemy, SpellSlot.E) >= enemy.Health))
                    {
                        E.Cast(enemy);
                    }
                }
            }
        }

        private static void sChoose()
        {
            var style = SubMenu["Misc"]["sID"].DisplayName;

            switch (style)
            {
                case "Skin1":
                    Player.SetSkinId(0);
                    break;
                case "Skin2":
                    Player.SetSkinId(1);
                    break;
                case "Skin3":
                    Player.SetSkinId(2);
                    break;
                case "Skin4":
                    Player.SetSkinId(3);
                    break;
                case "Skin5":
                    Player.SetSkinId(4);
                    break;
                case "Skin6":
                    Player.SetSkinId(5);
                    break;
            }
        }

        public static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + myHero.BoundingRadius);
        }

        private static void Obj_AI_Base_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            QReticles.Add(new QRecticle(sender, Utils.GameTimeTickCount + 2800));
            Core.DelayAction(() => { QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId); }, 2800);
        }
        
        public class QRecticle
        {
            public QRecticle(GameObject rectice, int expireTime)
            {
                this.Object = rectice;
                this.ExpireTime = expireTime;
            }
            public int ExpireTime { get; set; }
            public GameObject Object { get; set; }
            public Vector3 Position
            {
                get
                {
                    return this.Object.Position;
                }
            }
        }
    }
}
