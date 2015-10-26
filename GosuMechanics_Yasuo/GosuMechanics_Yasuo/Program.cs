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

namespace GosuMechanics_Yasuo
{
    class Program
    {
        public static bool wallCasted;
        public static YasWall wall = new YasWall();
        public static Menu menu;
        public static Spell.Skillshot SteelTempest;
        public static Spell.Targeted E;
        public static Spell.Skillshot W;
        public static Spell.Active R;
        public static Spell.Targeted Ignite;
        public static bool IsDashing = false;
        public static float startDash = 0;
        public static float time = 0;
        public static Vector3 castFrom;
        private static BuffType[] buffs;
        public static string[] interrupt;
        public static string[] notarget;
        public static string[] gapcloser;
        public static string Author = "WeAreGodz";
        public static string AddonName = "GosuMechanics Yasuo";
        public static Orbwalking.Orbwalker orbwalker;
        public static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }
        public static int[] abilitySequence;
        public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;
        public static List<Skillshot> DetectedSkillShots = new List<Skillshot>();
        public static List<Skillshot> EvadeDetectedSkillshots = new List<Skillshot>();
        public static float HealthPercent { get { return myHero.Health / myHero.MaxHealth * 100; } }

        internal class YasWall
        {
            public MissileClient pointL;
            public MissileClient pointR;
            public float endtime = 0;
            public YasWall()
            {

            }

            public YasWall(MissileClient L, MissileClient R)
            {
                pointL = L;
                pointR = R;
                endtime = Game.Time + 4;
            }

            public void setR(MissileClient R)
            {
                pointR = R;
                endtime = Game.Time + 4;
            }

            public void setL(MissileClient L)
            {
                pointL = L;
                endtime = Game.Time + 4;
            }

            public bool isValid(int time = 0)
            {
                return pointL != null && pointR != null && endtime - (time / 1000) > Game.Time;
            }
        }

        public static void Main(string[] args)
        {
            if (Hacks.RenderWatermark)
            {
                Hacks.RenderWatermark = false;
            }
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (myHero.Hero != Champion.Yasuo) { return; }
            Chat.Print("<font color=\"#F20000\"><b>GosuMechanics Yasuo:</b></font> Loaded!");

            SteelTempest = new Spell.Skillshot(SpellSlot.Q, 475, EloBuddy.SDK.Enumerations.SkillShotType.Linear, (int)250f, (int)8700f, (int)15f);
            W = new Spell.Skillshot(SpellSlot.W, 400, EloBuddy.SDK.Enumerations.SkillShotType.Cone);
            E = new Spell.Targeted(SpellSlot.E, 475);
            R = new Spell.Active(SpellSlot.R, 1200);
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }

            //Fluxy's TargetSelector2
            TargetSelector2.init();

            menu = EloBuddy.SDK.Menu.MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.0");
            menu.AddLabel(AddonName + " made by " + Author);
            menu.AddLabel("If you find some bugs, please do report it at my EloBuddy Add-On Thread.Thank You!");

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].Add("E", new CheckBox("Use E", true));
            SubMenu["Combo"].Add("E2", new Slider("minimum distance from enemy", 375, 0, 475));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].Add("E3", new Slider("E-GapCloser minimum distance from enemy", 475, 0, 1300));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddGroupLabel("White List Setting");
            foreach (var hero in EntityManager.Heroes.Enemies.Where(x => x.IsEnemy))
            {
                SubMenu["Combo"].Add(hero.ChampionName, new CheckBox("Use R if target is " + hero.ChampionName, true));
            }
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].Add("R", new CheckBox("Use Combo R", true));
            SubMenu["Combo"].Add("R2", new Slider("when enemy hp <=", 50, 0, 101));
            SubMenu["Combo"].Add("R3", new Slider("when x enemy is knocked", 2, 0, 5));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].Add("R4", new CheckBox("Use R Instantly when >= 1 ally is in Range", true));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].Add("Ignite", new CheckBox("Use Ignite", true));
            SubMenu["Combo"].Add("Items", new CheckBox("Use Items", true));
            SubMenu["Combo"].Add("myhp", new Slider("Use BOTRK if my HP is <=", 70, 0, 101));
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddSeparator();
            SubMenu["Combo"].AddGroupLabel("Will use Ato R if conditions are met");
            SubMenu["Combo"].Add("AutoR", new CheckBox("Use Auto R", true));
            SubMenu["Combo"].Add("AutoR2", new Slider("when x enemy is knocked", 3, 0, 5));
            SubMenu["Combo"].Add("AutoR2HP", new Slider("and my HP is >=", 101, 0, 101));
            SubMenu["Combo"].Add("AutoR2Enemies", new Slider("and Enemies in range <=", 2, 0, 5));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("AutoQ", new KeyBind("Auto Q Toggle", true, KeyBind.BindTypes.PressToggle, 'L'));
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Harass"].Add("Q3", new CheckBox("Use Q3", true));
            SubMenu["Harass"].Add("E", new CheckBox("Use E", true));
            SubMenu["Harass"].Add("QunderTower", new CheckBox("Auto Q UnderTower", true));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LastHit"].Add("Q3", new CheckBox("Use Q3", true));
            SubMenu["LastHit"].Add("E", new CheckBox("Use E", true));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");

            SubMenu["LaneClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("Q3", new CheckBox("Use Q3", true));
            SubMenu["LaneClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["LaneClear"].Add("Items", new CheckBox("Use Items", true));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");

            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Items", new CheckBox("Use Items", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].AddGroupLabel("AntiGapcloser/Interrupt Settings");
            SubMenu["Misc"].Add("UseEInterrupt", new CheckBox("Use E to Interrupt/Antigapcloser", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Escape Setting");
            SubMenu["Misc"].Add("EscQ", new CheckBox("Use Q", true));
            SubMenu["Misc"].Add("EscE", new CheckBox("Use E", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].Add("noEturret", new CheckBox("Dont Jump Turret", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("KillSteal Setting");
            SubMenu["Misc"].Add("KsQ", new CheckBox("Use Q", true));
            SubMenu["Misc"].Add("KsE", new CheckBox("Use E", true));
            SubMenu["Misc"].Add("KsIgnite", new CheckBox("Use Ignite", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Auto Level Setting");
            SubMenu["Misc"].Add("LevelUp", new CheckBox("Enable AutoLevel", true));

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Skin Setting");
            var skin = SubMenu["Misc"].Add("sID", new Slider("Skin", 1, 0, 2));
            var sID = new[] { "Classic", "High-Noon Yasuo", "Project Yasuo" };
            skin.DisplayName = sID[skin.CurrentValue];

            skin.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = sID[changeArgs.NewValue];
                };

            SubMenu["Misc"].AddSeparator(10);
            SubMenu["Misc"].AddGroupLabel("Draw Setting");
            SubMenu["Misc"].Add("DrawQ", new CheckBox("Draw Q", true));
            SubMenu["Misc"].Add("DrawQ3", new CheckBox("Draw Q3", true));
            SubMenu["Misc"].Add("DrawE", new CheckBox("Draw E", true));
            SubMenu["Misc"].Add("DrawR", new CheckBox("Draw R", true));
            SubMenu["Misc"].Add("DrawTarget", new CheckBox("Draw Target", true));

            SubMenu["aShots"] = menu.AddSubMenu("Evade Settings", "aShots");
            //SmartW
            SubMenu["aShots"].Add("smartW", new CheckBox("Smart WindWall", true));
            SubMenu["aShots"].Add("smartWD", new Slider("Smart WindWall Delay(cast when SPELL is about to hit in x milliseconds)", 3000, 0, 3000));
            SubMenu["aShots"].Add("smartEDogue", new CheckBox("E use Dodge", true));
            //SubMenu["aShots"].Add("WW", new CheckBox("Use WindWall", true));
            SubMenu["aShots"].Add("wwDanger", new CheckBox("WindWall only dangerous", true));
            //SubMenu["aShots"].Add("wwDmg", new Slider("WW if does proc HP", 0, 100, 1));
            //Create the skillshots submenus.
            var skillShots = MainMenu.AddMenu("Enemy Skillshots", "aShotsSkills");

            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.Team != ObjectManager.Player.Team)
                {
                    foreach (var spell in SpellDatabase.Spells)
                    {
                        if (spell.ChampionName == hero.ChampionName)
                        {
                            SubMenu["SMIN"] = skillShots.AddSubMenu(spell.MenuItemName, spell.MenuItemName);

                            SubMenu["SMIN"].Add
                                ("DangerLevel" + spell.MenuItemName,
                                    new Slider("Danger level", spell.DangerValue, 5, 1));

                            SubMenu["SMIN"].Add
                                ("IsDangerous" + spell.MenuItemName,
                                    new CheckBox("Is Dangerous", spell.IsDangerous));

                            //SubMenu["SMIN"].Add("Draw" + spell.MenuItemName, new CheckBox("Draw", true));
                            SubMenu["SMIN"].Add("Enabled" + spell.MenuItemName, new CheckBox("Enabled", true));
                        }
                    }
                }
            }

            SubMenu["QSS"] = menu.AddSubMenu("Auto QSS/Mercurial", "QSS");
            SubMenu["QSS"].AddGroupLabel("Auto QSS/Mercurial Settings");
            SubMenu["QSS"].Add("use", new KeyBind("Use QSS/Mercurial", true, KeyBind.BindTypes.PressToggle, 'K'));
            SubMenu["QSS"].Add("delay", new Slider("Activation Delay", 1000, 0, 2000));

            SubMenu["gap"] = menu.AddSubMenu("Gapcloser List", "gap");
            SubMenu["gap2"] = menu.AddSubMenu("Gapcloser List 2", "gap2");
            SubMenu["int"] = menu.AddSubMenu("Interrupt List", "int");

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


            gapcloser = new[]
            {
                "AkaliShadowDance", "Headbutt", "DianaTeleport", "IreliaGatotsu", "JaxLeapStrike", "JayceToTheSkies",
                "MaokaiUnstableGrowth", "MonkeyKingNimbus", "Pantheon_LeapBash", "PoppyHeroicCharge", "QuinnE",
                "XenZhaoSweep", "blindmonkqtwo", "FizzPiercingStrike", "RengarLeap"
            };
            notarget = new[]
            {
                "AatroxQ", "GragasE", "GravesMove", "HecarimUlt", "JarvanIVDragonStrike", "JarvanIVCataclysm", "KhazixE",
                "khazixelong", "LeblancSlide", "LeblancSlideM", "LeonaZenithBlade", "UFSlash", "RenektonSliceAndDice",
                "SejuaniArcticAssault", "ShenShadowDash", "RocketJump", "slashCast"
            };
            interrupt = new[]
            {
                "KatarinaR", "GalioIdolOfDurand", "Crowstorm", "Drain", "AbsoluteZero", "ShenStandUnited", "UrgotSwap2",
                "AlZaharNetherGrasp", "FallenOne", "Pantheon_GrandSkyfall_Jump", "VarusQ", "CaitlynAceintheHole",
                "MissFortuneBulletTime", "InfiniteDuress", "LucianR"
            };
            for (int i = 0; i < gapcloser.Length; i++)
            {
                SubMenu["gap"].Add(gapcloser[i], new CheckBox(gapcloser[i], true));
            }
            for (int i = 0; i < notarget.Length; i++)
            {
                SubMenu["gap2"].Add(notarget[i], new CheckBox(notarget[i], true));
            }
            for (int i = 0; i < interrupt.Length; i++)
            {
                SubMenu["int"].Add(interrupt[i], new CheckBox(interrupt[i], true));
            }

            orbwalker = new Orbwalking.Orbwalker(menu);

            if (myHero.ChampionName == "Yasuo") abilitySequence = new int[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 3, 3, 2, 2, 4, 2, 2 };

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            SkillshotDetector.OnDetectSkillshot += Evade_OnDetectSkillshot;
            SkillshotDetector.OnDeleteMissile += Evade_OnDeleteMissile;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            Obj_AI_Base.OnDelete += Obj_AI_Base_OnDelete;
            Spellbook.OnStopCast += Spellbook_OnStopCast;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Obj_AI_Base.OnBuffLose += Obj_AI_Base_OnBuffLose;
        }

        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs buff)
        {
            if (sender != null && sender.IsMe && sender.IsValid)
            {
                if (buff.Buff.Name == "yasuoq3w")
                {
                    SteelTempest = new Spell.Skillshot(SpellSlot.Q, 1000, EloBuddy.SDK.Enumerations.SkillShotType.Linear, (int)250f, (int)1200f, (int)90f);
                }
            }
        }

        private static void Obj_AI_Base_OnBuffLose(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs buff)
        {
            if (sender != null && sender.IsMe && sender.IsValid)
            {
                if (buff.Buff.Name == "yasuoq3w")
                {
                    SteelTempest = new Spell.Skillshot(SpellSlot.Q, 475, EloBuddy.SDK.Enumerations.SkillShotType.Linear, (int)250f, (int)1800f, (int)15f);
                }
            }
        }

        private static void Spellbook_OnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (sender.IsValid && args.DestroyMissile && args.StopAnimation)
                {
                    IsDashing = false;
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("YasuoQW"))
            {
                Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
            }
            if (args.SData.Name.ToLower().Contains("yasuoq2w"))
            {
                Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
            }
            if (args.SData.Name.ToLower().Contains("yasuoq3w"))
            {
                Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
            }

            if (sender.IsMe)
            {
                if (args.SData.Name == "YasuoDashWrapper")
                {
                    Console.WriteLine("starting dash");
                    IsDashing = true;
                    castFrom = myHero.Position;
                    startDash = Environment.TickCount;
                }
            }
            if (Q3READY(myHero) && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && interrupt.Any(x => x.Contains(args.SData.Name)) &&
               SubMenu["int"][args.SData.Name].Cast<CheckBox>().CurrentValue && myHero.Distance(sender) <= 900)
            {
                SteelTempest.Cast(sender);
            }

            if (Q3READY(myHero) && gapcloser.Any(str => str.Contains(args.SData.Name)) && SubMenu["gap"][args.SData.Name].Cast<CheckBox>().CurrentValue && args.Target.IsMe)
            {
                SteelTempest.Cast(sender);
            }

            if (Q3READY(myHero) && notarget.Any(str => str.Contains(args.SData.Name)) &&
                Vector3.Distance(args.End, ObjectManager.Player.Position) <= 300 && sender.IsValidTarget(900) &&
                SubMenu["gap2"][args.SData.Name].Cast<CheckBox>().CurrentValue)
            {
                if (ObjectManager.Player.Distance(args.End) < ObjectManager.Player.Distance(sender.Position))
                {
                    SteelTempest.Cast(sender);
                }
            }

            if (sender is AIHeroClient)
            {
                var pant = (AIHeroClient)sender;
                if (pant.IsValidTarget() && pant.ChampionName == "Pantheon" && pant.GetSpellSlotFromName(args.SData.Name) == SpellSlot.W)
                {
                    if (Q3READY(myHero) && SubMenu["Misc"]["UseEInterrupt"].Cast<CheckBox>().CurrentValue && args.Target.IsMe)
                    {
                        if (pant.IsValidTarget(E.Range))
                        {
                            SteelTempest.Cast(pant);
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

        private static void Obj_AI_Base_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                MissileClient missle = (MissileClient)sender;
                if (missle.SData.Name == "yasuowmovingwallmisl")
                {
                    wall.setL(missle);
                }
                if (missle.SData.Name == "yasuowmovingwallmisl")
                {
                    wallCasted = false;
                }
                if (missle.SData.Name == "yasuowmovingwallmisr")
                {
                    wall.setR(missle);
                }
            }
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is MissileClient)
            {
                MissileClient missle = (MissileClient)sender;
                if (missle.SData.Name == "yasuowmovingwallmisl")
                {
                    wall.setL(missle);
                }
                if (missle.SData.Name == "yasuowmovingwallmisl")
                {
                    wallCasted = true;
                }
                if (missle.SData.Name == "yasuowmovingwallmisr")
                {
                    wall.setR(missle);
                }
            }
        }

        public static bool skillShotIsDangerous(string Name)
        {
            if (SubMenu["SMIN"]["IsDangerous" + Name] != null)
            {
                return SubMenu["SMIN"]["IsDangerous" + Name].Cast<CheckBox>().CurrentValue;
            }
            return true;
        }

        public static bool EvadeSpellEnabled(string Name)
        {
            if (SubMenu["SMIN"]["Enabled" + Name] != null)
            {
                return SubMenu["SMIN"]["Enabled" + Name].Cast<CheckBox>().CurrentValue;
            }
            return true;
        }

        public static void updateSkillshots()
        {
            foreach (var ss in EvadeDetectedSkillshots)
            {
                ss.Game_OnGameUpdate();
            }
        }

        public static void useWSmart(Skillshot skillShot)
        {
            //try douge with E if cant windWall
            var delay = SubMenu["aShots"]["smartWD"].Cast<Slider>().CurrentValue;
            if (skillShot.IsAboutToHit(delay, myHero))
            {
                if (!W.IsReady() || skillShot.SpellData.Type == SkillShotType.SkillshotRing)
                    return;

                var sd = SpellDatabase.GetByMissileName(skillShot.SpellData.MissileSpellName);
                if (sd == null)
                    return;

                //If enabled
                if (!EvadeSpellEnabled(sd.MenuItemName))
                    return;

                //if only dangerous
                if (SubMenu["aShots"]["wwDanger"].Cast<CheckBox>().CurrentValue &&
                    !skillShotIsDangerous(sd.MenuItemName))
                    return;

                myHero.Spellbook.CastSpell(SpellSlot.W, skillShot.Start.To3D(), skillShot.End.To3D());
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (SubMenu["Misc"]["LevelUp"].Cast<CheckBox>().CurrentValue)
            {
                int qL = myHero.Spellbook.GetSpell(SpellSlot.Q).Level + qOff;
                int wL = myHero.Spellbook.GetSpell(SpellSlot.W).Level + wOff;
                int eL = myHero.Spellbook.GetSpell(SpellSlot.E).Level + eOff;
                int rL = myHero.Spellbook.GetSpell(SpellSlot.R).Level + rOff;
                if (qL + wL + eL + rL < ObjectManager.Player.Level)
                {
                    int[] level = new int[] { 0, 0, 0, 0 };
                    for (int i = 0; i < ObjectManager.Player.Level; i++)
                    {
                        level[abilitySequence[i] - 1] = level[abilitySequence[i] - 1] + 1;
                    }
                    if (qL < level[0]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                    if (wL < level[1]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                    if (eL < level[2]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                    if (rL < level[3]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
                }
            }

            Mode.KillSteal();
            Mode.AutoR();
            Mode.sChoose();

            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Mode.Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Mode.Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Mode.LastHit();
                    break;
                case Orbwalking.OrbwalkingMode.Clear:
                    Mode.LaneClear();
                    Mode.JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.Flee:
                    Mode.Flee();
                    break;
            }

            if (SubMenu["Harass"]["AutoQ"].Cast<KeyBind>().CurrentValue || orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Flee && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Flee)
            {
                var TsTarget = TargetSelector2.GetTarget(SteelTempest.Range, DamageType.Physical);
                orbwalker.ForceTarget(TsTarget);

                if (TsTarget == null)
                {
                    return;
                }

                if (TsTarget != null && SubMenu["Harass"]["QunderTower"].Cast<CheckBox>().CurrentValue)
                {
                    PredictionResult QPred = Program.SteelTempest.GetPrediction(TsTarget);

                    if (SteelTempest.IsReady() && SteelTempest.Range == 1000 && SubMenu["Harass"]["Q3"].Cast<CheckBox>().CurrentValue && !isDashing())
                    {
                        SteelTempest.Cast(QPred.CastPosition);
                    }
                    else if (!Q3READY(myHero) && SteelTempest.IsReady() && SteelTempest.Range == 475 && SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue && !isDashing())
                    {
                        SteelTempest.Cast(QPred.CastPosition);
                    }
                }
                else if (TsTarget != null && !SubMenu["Harass"]["QunderTower"].Cast<CheckBox>().CurrentValue)
                {
                    PredictionResult QPred = Program.SteelTempest.GetPrediction(TsTarget);

                    if (!UnderTower(myHero.ServerPosition) && SteelTempest.IsReady() && SteelTempest.Range == 1000 && SubMenu["Harass"]["Q3"].Cast<CheckBox>().CurrentValue && !isDashing())
                    {
                        SteelTempest.Cast(QPred.CastPosition);
                    }
                    if (!Q3READY(myHero) && SteelTempest.IsReady() && SteelTempest.Range == 475 && SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue && !isDashing() && !UnderTower(myHero.ServerPosition))
                    {
                        SteelTempest.Cast(QPred.CastPosition);
                    }
                }

                if (startDash + 470000 / ((700 + myHero.MoveSpeed)) < Environment.TickCount && isDashing())
                {
                    IsDashing = false;
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
            }

            EvadeDetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());

            foreach (var mis in EvadeDetectedSkillshots)
            {
                if (SubMenu["aShots"]["smartW"].Cast<CheckBox>().CurrentValue && !isSafePoint(myHero.Position.To2D(), true).IsSafe)
                {
                    useWSmart(mis);
                }

                if (SubMenu["aShots"]["smartEDogue"].Cast<CheckBox>().CurrentValue && !isSafePoint(myHero.Position.To2D(), true).IsSafe)
                {
                    useEtoSafe(mis);
                }
            }
        }

        public struct IsSafeResult
        {
            public bool IsSafe;
            public List<Skillshot> SkillshotList;
            public List<Obj_AI_Base> casters;
        }


        public static IsSafeResult isSafePoint(Vector2 point, bool igonre = false)
        {
            var result = new IsSafeResult();
            result.SkillshotList = new List<Skillshot>();
            result.casters = new List<Obj_AI_Base>();


            if (orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo ||
                             point.To3D().CountEnemiesInRange(500) > myHero.HealthPercent % 65)
            {
                result.IsSafe = false;
                return result;
            }

            foreach (var skillshot in EvadeDetectedSkillshots)
            {
                if (skillshot.IsDanger(point) && skillshot.IsAboutToHit(500, myHero))
                {
                    result.SkillshotList.Add(skillshot);
                    result.casters.Add(skillshot.Unit);
                }
            }

            result.IsSafe = (result.SkillshotList.Count == 0);
            return result;
        }

        public static Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2,
                Vector2 pe2)
        {
            // Get A,B,C of first line - points : ps1 to pe1
            float A1 = pe1.Y - ps1.Y;
            float B1 = ps1.X - pe1.X;
            float C1 = A1 * ps1.X + B1 * ps1.Y;

            // Get A,B,C of second line - points : ps2 to pe2
            float A2 = pe2.Y - ps2.Y;
            float B2 = ps2.X - pe2.X;
            float C2 = A2 * ps2.X + B2 * ps2.Y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
                return new Vector2(-1, -1);

            // now return the Vector2 intersection point
            return new Vector2(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            );
        }

        public static bool willColide(Skillshot ss, Vector2 from, float speed, Vector2 direction, float radius)
        {
            Vector2 ssVel = ss.Direction.Normalized() * ss.SpellData.MissileSpeed;
            Vector2 dashVel = direction * speed;
            Vector2 a = ssVel - dashVel;
            Vector2 realFrom = from.Extend(direction, ss.SpellData.Delay + speed);
            if (!ss.IsAboutToHit((int)((dashVel.Length() / 475) * 1000) + Game.Ping + 100, ObjectManager.Player))
                return false;
            if (ss.IsAboutToHit(1000, ObjectManager.Player) && interCir(ss.MissilePosition, ss.MissilePosition.Extend(ss.MissilePosition + a, ss.SpellData.Range + 50), from,
                radius))
                return true;
            return false;
        }

        public static bool interCir(Vector2 E, Vector2 L, Vector2 C, float r)
        {
            Vector2 d = L - E;
            Vector2 f = E - C;

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - r * r;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {

            }
            else
            {

                discriminant = (float)Math.Sqrt(discriminant);

                float t1 = (-b - discriminant) / (2 * a);
                float t2 = (-b + discriminant) / (2 * a);

                if (t1 >= 0 && t1 <= 1)
                {
                    return true;
                }

                if (t2 >= 0 && t2 <= 1)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return (from + distance * Vector3.Normalize(direction - from)).To2D();
        }

        public static bool wontHitOnDash(Skillshot ss, Obj_AI_Base jumpOn, Skillshot skillShot, Vector2 dashDir)
        {
            float currentDashSpeed = 700 + myHero.MoveSpeed;//At least has to be like this

            Vector2 intersectionPoint = LineIntersectionPoint(myHero.Position.To2D(), V2E(myHero.Position, jumpOn.Position, 475), ss.Start, ss.End);

            float arrivingTime = Vector2.Distance(myHero.Position.To2D(), intersectionPoint) / currentDashSpeed;

            Vector2 skillshotPosition = ss.GetMissilePosition((int)(arrivingTime * 1000));
            if (Vector2.DistanceSquared(skillshotPosition, intersectionPoint) <
                (ss.SpellData.Radius + myHero.BoundingRadius) && !willColide(skillShot, myHero.Position.To2D(), 700f + myHero.MoveSpeed, dashDir, myHero.BoundingRadius + skillShot.SpellData.Radius))
                return false;
            return true;
        }

        public static void useEtoSafe(Skillshot skillShot)
        {
            if (!E.IsReady())
                return;
            float closest = float.MaxValue;
            Obj_AI_Base closestTarg = null;
            float currentDashSpeed = 700 + myHero.MoveSpeed;
            foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(ob => ob.NetworkId != skillShot.Unit.NetworkId && enemyIsJumpable(ob) && ob.Distance(myHero) < E.Range).OrderBy(ene => ene.Distance(Game.CursorPos, true)))
            {
                var pPos = myHero.Position.To2D();
                Vector2 posAfterE = V2E(myHero.Position, enemy.Position, 475);
                Vector2 dashDir = (posAfterE - myHero.Position.To2D()).Normalized();

                if (isSafePoint(posAfterE).IsSafe && wontHitOnDash(skillShot, enemy, skillShot, dashDir) /*&& skillShot.IsSafePath(new List<Vector2>() { posAfterE }, 0, (int)currentDashSpeed, 0).IsSafe*/)
                {
                    float curDist = Vector2.DistanceSquared(Game.CursorPos.To2D(), posAfterE);
                    if (curDist < closest)
                    {
                        closestTarg = enemy;
                        closest = curDist;
                    }
                }
            }
            if (closestTarg != null)
                useENormal(closestTarg);
        }

        public static bool enemyIsJumpable(Obj_AI_Base enemy, List<AIHeroClient> ignore = null)
        {
            if (enemy.IsValid && enemy.IsEnemy && !enemy.IsInvulnerable && !enemy.MagicImmune && !enemy.IsDead && !(enemy is FollowerObject))
            {
                if (ignore != null)
                    foreach (AIHeroClient ign in ignore)
                    {
                        if (ign.NetworkId == enemy.NetworkId)
                            return false;
                    }
                foreach (BuffInstance buff in enemy.Buffs)
                {
                    if (buff.Name == "YasuoDashWrapper")
                        return false;
                }
                return true;
            }
            return false;
        }

        public static bool useENormal(Obj_AI_Base target)
        {
            if (!E.IsReady() || target.Distance(myHero) > 470)
                return false;
            Vector2 posAfter = V2E(myHero.Position, target.Position, 475);
            if (!SubMenu["Misc"]["noEturret"].Cast<CheckBox>().CurrentValue)
            {
                if (isSafePoint(posAfter).IsSafe)
                {

                    E.Cast(target);
                }
                return true;
            }
            else
            {
                Vector2 pPos = myHero.ServerPosition.To2D();
                Vector2 posAfterE = pPos + (Vector2.Normalize(target.Position.To2D() - pPos) * E.Range);
                if (!UnderEnemyTower(PosAfterE(target)))
                {
                    Console.WriteLine("use gap?");
                    if (isSafePoint(posAfter, true).IsSafe)
                    {

                        E.Cast(target);
                    }
                    return true;
                }
            }
            return false;

        }

        public static Vector2 getNextPos(AIHeroClient target)
        {
            Vector2 dashPos = target.Position.To2D();
            if (target.IsMoving && target.Path.Count() != 0)
            {
                Vector2 tpos = target.Position.To2D();
                Vector2 path = target.Path[0].To2D() - tpos;
                path.Normalize();
                dashPos = tpos + (path * 100);
            }
            return dashPos;
        }

        public static void putWallBehind(AIHeroClient target)
        {
            if (!W.IsReady() || !E.IsReady() || target.IsMelee)
                return;
            Vector2 dashPos = getNextPos(target);
            //PredictionResult po = Prediction.Position.PredictUnitPosition(, (int)0.5f);

            float dist = myHero.Distance(target.Position);
            if (!target.IsMoving || myHero.Distance(dashPos) <= dist + 40)
                if (dist < 330 && dist > 100 && W.IsReady())
                {
                    W.Cast(target.Position);
                }
        }

        public static void eBehindWall(AIHeroClient target)
        {
            if (!E.IsReady() || !enemyIsJumpable(target) || target.IsMelee)
                return;
            float dist = myHero.Distance(target);
            var pPos = myHero.Position.To2D();
            Vector2 dashPos = target.Position.To2D();
            if (!target.IsMoving || myHero.Distance(dashPos) <= dist)
            {
                foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy)))
                {
                    Vector2 posAfterE = pPos + (Vector2.Normalize(enemy.Position.To2D() - pPos) * E.Range);
                    if ((target.Distance(posAfterE) < dist
                        || target.Distance(posAfterE) < myHero.GetAutoAttackRange(target) + 100)
                        && goesThroughWall(target.Position, posAfterE.To3D()))
                    {
                        if (E.Cast(target))
                            return;
                    }
                }
            }
        }

        public static bool goesThroughWall(Vector3 vec1, Vector3 vec2)
        {
            if (wall.endtime < Game.Time || wall.pointL == null || wall.pointL == null)
                return false;
            Vector2 inter = LineIntersectionPoint(vec1.To2D(), vec2.To2D(), wall.pointL.Position.To2D(), wall.pointR.Position.To2D());
            float wallW = (300 + 50 * W.Level);
            if (wall.pointL.Position.To2D().Distance(inter) > wallW ||
                wall.pointR.Position.To2D().Distance(inter) > wallW)
                return false;
            var dist = vec1.Distance(vec2);
            if (vec1.To2D().Distance(inter) + vec2.To2D().Distance(inter) - 30 > dist)
                return false;

            return true;
        }

        private static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (SubMenu["Misc"]["DrawQ"].Cast<CheckBox>().CurrentValue && SteelTempest.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, SteelTempest.Range, System.Drawing.Color.Green);
            }
            if (SubMenu["Misc"]["DrawQ3"].Cast<CheckBox>().CurrentValue && SteelTempest.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, 1100, System.Drawing.Color.Green);
            }
            if (SubMenu["Misc"]["DrawE"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, E.Range, System.Drawing.Color.Green);
            }
            if (SubMenu["Misc"]["DrawR"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, R.Range, System.Drawing.Color.Green);
            }
            if (SubMenu["Misc"]["DrawQ"].Cast<CheckBox>().CurrentValue && !SteelTempest.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, SteelTempest.Range, System.Drawing.Color.Red);
            }
            if (SubMenu["Misc"]["DrawQ3"].Cast<CheckBox>().CurrentValue && !SteelTempest.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, 1100, System.Drawing.Color.Red);
            }
            if (SubMenu["Misc"]["DrawE"].Cast<CheckBox>().CurrentValue && !E.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, E.Range, System.Drawing.Color.Red);
            }
            if (SubMenu["Misc"]["DrawR"].Cast<CheckBox>().CurrentValue && !R.IsReady())
            {
                Drawing.DrawCircle(myHero.Position, R.Range, System.Drawing.Color.Red);
            }
        }

        public static bool isDashing()
        {
            return IsDashing;
        }

        public static bool Q3READY(AIHeroClient unit)
        {
            return myHero.HasBuff("YasuoQ3W");
        }

        public static bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        public static bool IsKnockedUp(AIHeroClient target)
        {
            return target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback);
        }

        public static bool CanCastDelayR(AIHeroClient target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockback || i.Type == BuffType.Knockup);
            return buff != null && buff.EndTime - Game.Time <= (buff.EndTime - buff.StartTime) / 3;
        }



        public static bool AlliesNearTarget(Obj_AI_Base target, float range)
        {
            return EntityManager.Heroes.Allies.Where(tar => tar.Distance(target) < range).Any(tar => tar != null);
        }

        public static Vector2 PosAfterE(Obj_AI_Base target)
        {
            if (!target.IsValidTarget())
            {
                return Vector2.Zero;
            }

            var baseX = myHero.Position.X;
            var baseY = myHero.Position.Y;
            var targetX = target.Position.X;
            var targetY = target.Position.Y;

            var vector = new Vector2(targetX - baseX, targetY - baseY);
            var sqrt = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);

            var x = (float)(baseX + (E.Range * (vector.X / sqrt)));
            var y = (float)(baseY + (E.Range * (vector.Y / sqrt)));

            return new Vector2(x, y);
        }

        public static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + myHero.BoundingRadius);
        }

        public static bool UnderEnemyTower(Vector2 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(i => !i.IsDead && i.Distance(pos) <= 900 + myHero.BoundingRadius);
        }

        public static void UseItems(Obj_AI_Base unit)
        {
            if (Item.HasItem((int)ItemId.Blade_of_the_Ruined_King, myHero) && Item.CanUseItem((int)ItemId.Blade_of_the_Ruined_King)
               && SubMenu["Combo"]["Items"].Cast<CheckBox>().CurrentValue && HealthPercent <= SubMenu["Combo"]["myhp"].Cast<Slider>().CurrentValue)
            {
                Item.UseItem((int)ItemId.Blade_of_the_Ruined_King, unit);
            }
            if (Item.HasItem((int)ItemId.Bilgewater_Cutlass, myHero) && Item.CanUseItem((int)ItemId.Bilgewater_Cutlass)
               && unit.IsValidTarget())
            {
                Item.UseItem((int)ItemId.Bilgewater_Cutlass, unit);
            }
            if (Item.HasItem((int)ItemId.Youmuus_Ghostblade, myHero) && Item.CanUseItem((int)ItemId.Youmuus_Ghostblade)
               && myHero.Distance(unit.Position) <= myHero.GetAutoAttackRange())
            {
                Item.UseItem((int)ItemId.Youmuus_Ghostblade);
            }
            if (Item.HasItem((int)ItemId.Ravenous_Hydra_Melee_Only, myHero) && Item.CanUseItem((int)ItemId.Ravenous_Hydra_Melee_Only)
               && myHero.Distance(unit.Position) <= 400)
            {
                Item.UseItem((int)ItemId.Ravenous_Hydra_Melee_Only);
            }
            if (Item.HasItem((int)ItemId.Tiamat_Melee_Only, myHero) && Item.CanUseItem((int)ItemId.Tiamat_Melee_Only)
               && myHero.Distance(unit.Position) <= 400)
            {
                Item.UseItem((int)ItemId.Tiamat_Melee_Only);
            }
            if (Item.HasItem((int)ItemId.Randuins_Omen, myHero) && Item.CanUseItem((int)ItemId.Randuins_Omen)
               && myHero.Distance(unit.Position) <= 400)
            {
                Item.UseItem((int)ItemId.Randuins_Omen);
            }
        }

        private static void Evade_OnDetectSkillshot(Skillshot skillshot)
        {
            //Check if the skillshot is already added.
            var alreadyAdded = false;

            foreach (var item in EvadeDetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                }
            }

            //Check if the skillshot is from an ally.
            if (skillshot.Unit.Team == ObjectManager.Player.Team)
            {
                return;
            }

            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(ObjectManager.Player.ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }

            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded)
            {
                //Multiple skillshots like twisted fate Q.
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            EvadeDetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        const int angle = 60;
                        const int fraction = -angle / 2;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                fraction * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float)Math.PI / 180);

                        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            var v = minion.ServerPosition.To2D() - skillshot.Unit.ServerPosition.To2D();
                            if (minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Unit) < 800 &&
                                (minion.Team != ObjectManager.Player.Team))
                            {
                                var start = minion.ServerPosition.To2D();
                                var end = skillshot.Unit.ServerPosition.To2D()
                                    .Extend(
                                        minion.ServerPosition.To2D(),
                                        skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000);

                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Unit);
                                EvadeDetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;


                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");

                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        EvadeDetectedSkillshots.Add(bounce1);
                        EvadeDetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }

                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var endCSpellData = SpellDatabase.GetByName("OriannaQend");

                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, endCSpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    EvadeDetectedSkillshots.Add(skillshotToAdd);
                }


                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }

                EvadeDetectedSkillshots.Add(skillshot);
            }
        }

        private static void Evade_OnDeleteMissile(Skillshot skillshot, MissileClient missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();
                if (EvadeDetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (var i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Environment.TickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i * direction * spellData.Range, skillshot.Unit);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }
    }
}