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
    class Mode
    {
        public static void AutoR()
        {
            if (!Program.R.IsReady())
            {
                return;
            }

            var useR = Program.SubMenu["Combo"]["AutoR"].Cast<CheckBox>().CurrentValue;
            var autoREnemies = Program.SubMenu["Combo"]["AutoR2"].Cast<Slider>().CurrentValue;
            var MyHP = Program.SubMenu["Combo"]["AutoR2HP"].Cast<Slider>().CurrentValue;
            var enemyInRange = Program.SubMenu["Combo"]["AutoR2Enemies"].Cast<Slider>().CurrentValue;
            //var useRDown = SubMenu["Combo"]["AutoR3"].Cast<Slider>().CurrentValue;

            if (!useR)
            {
                return;
            }

            var enemiesKnockedUp =
                ObjectManager.Get<AIHeroClient>()
                    .Where(x => x.IsValidTarget(Program.R.Range))
                    .Where(x => x.HasBuffOfType(BuffType.Knockup));

            var enemies = enemiesKnockedUp as IList<AIHeroClient> ?? enemiesKnockedUp.ToList();

            if (enemies.Count() >= autoREnemies && Program.myHero.Health >= MyHP && Program.myHero.CountEnemiesInRange(1500) <= enemyInRange)
            {
                Program.R.Cast();
            }
        }

        public static void Combo()
        {
            var TsTarget = TargetSelector2.GetTarget(1300, DamageType.Physical);
            Program.orbwalker.ForceTarget(TsTarget);

            if (TsTarget == null)
            {
                return;
            }

            if (TsTarget != null)
            {
                if (Program.SteelTempest.IsReady() && Program.SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue)
                {
                    PredictionResult QPred = Program.SteelTempest.GetPrediction(TsTarget);
                    if (!Program.isDashing() && Program.SteelTempest.Range == 1000)
                    {
                        Program.SteelTempest.Cast(QPred.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    }
                    else if (Program.SteelTempest.Range == 1000 && Program.Q3READY(Program.myHero) && Program.isDashing() && Program.myHero.Distance(TsTarget) <= 250 * 250)
                    {
                        Program.SteelTempest.Cast(QPred.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    }
                    else if (!Program.Q3READY(Program.myHero) && Program.SteelTempest.Range == 475 )
                    {
                        Program.SteelTempest.Cast(QPred.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    }
                }

                if (Program.SubMenu["Combo"]["Items"].Cast<CheckBox>().CurrentValue && TsTarget.IsValidTarget())
                {
                    Program.UseItems(TsTarget);
                }
                if (Program.E.IsReady() && Program.SubMenu["Combo"]["E"].Cast<CheckBox>().CurrentValue)
                {
                    if (Program.SubMenu["aShots"]["smartW"].Cast<CheckBox>().CurrentValue)
                    {
                        Program.putWallBehind(TsTarget);
                    }
                    if (Program.SubMenu["aShots"]["smartW"].Cast<CheckBox>().CurrentValue && Program.wallCasted && Program.myHero.Distance(TsTarget.Position) < 300)
                    {
                        Program.eBehindWall(TsTarget);
                    }

                    if (Program.SubMenu["Misc"]["noEturret"].Cast<CheckBox>().CurrentValue && !Program.UnderEnemyTower(Program.PosAfterE(TsTarget)) && TsTarget.Distance(Program.myHero) >= (Program.SubMenu["Combo"]["E2"].Cast<Slider>().CurrentValue) && Program.CanCastE(TsTarget) && Program.myHero.IsFacing(TsTarget))
                    {
                        Program.E.Cast(TsTarget);
                    }
                    else if (Program.SteelTempest.IsReady() && Program.isDashing() && Program.myHero.Distance(TsTarget) <= 275 * 275)
                    {
                        PredictionResult Qpred = Program.SteelTempest.GetPrediction(TsTarget);
                        Core.DelayAction(() => { Program.SteelTempest.Cast(Qpred.CastPosition); }, 200);
                    }

                    var bestMinion =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(x => x.IsValidTarget(Program.E.Range))
                        .Where(x => x.Distance(TsTarget) < Program.myHero.Distance(TsTarget))
                        .OrderByDescending(x => x.Distance(Program.myHero))
                        .FirstOrDefault();

                    if (Program.SubMenu["Misc"]["noEturret"].Cast<CheckBox>().CurrentValue && bestMinion != null && !Program.UnderEnemyTower(Program.PosAfterE(bestMinion)) && Program.myHero.IsFacing(bestMinion) && TsTarget.Distance(Program.myHero) >= (Program.SubMenu["Combo"]["E3"].Cast<Slider>().CurrentValue) && Program.CanCastE(bestMinion) && Program.myHero.IsFacing(bestMinion))
                    {
                        Program.E.Cast(bestMinion);
                    }

                    foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myHero.ServerPosition, Program.E.Range, true))
                    {
                        var bestJMinion =
                           ObjectManager.Get<Obj_AI_Base>()
                               .Where(x => x.IsValidTarget(Program.E.Range))
                               .Where(x => x.Distance(TsTarget) < Program.myHero.Distance(TsTarget))
                               .OrderByDescending(x => x.Distance(Program.myHero))
                               .FirstOrDefault();

                        if (Program.SubMenu["Misc"]["noEturret"].Cast<CheckBox>().CurrentValue && !Program.UnderEnemyTower(Program.PosAfterE(bestJMinion)) && bestJMinion != null && Program.myHero.IsFacing(bestJMinion) && Program.E.IsReady() && TsTarget.Distance(Program.myHero) >= (Program.SubMenu["Combo"]["E3"].Cast<Slider>().CurrentValue) && Program.myHero.IsFacing(bestJMinion))
                        {
                            Program.E.Cast(bestJMinion);
                        }
                    }
                    foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
                    {
                        var bestEnemy =
                          ObjectManager.Get<Obj_AI_Base>()
                              .Where(x => x.IsValidTarget(Program.E.Range))
                              .Where(x => x.Distance(TsTarget) < Program.myHero.Distance(TsTarget))
                              .OrderByDescending(x => x.Distance(Program.myHero))
                              .FirstOrDefault();

                        if (Program.SubMenu["Misc"]["noEturret"].Cast<CheckBox>().CurrentValue && !Program.UnderEnemyTower(Program.PosAfterE(bestEnemy)) && bestEnemy != null && Program.myHero.IsFacing(bestEnemy) && Program.E.IsReady() && TsTarget.Distance(Program.myHero) >= (Program.SubMenu["Combo"]["E3"].Cast<Slider>().CurrentValue) && Program.myHero.IsFacing(bestEnemy))
                        {
                            Program.E.Cast(bestEnemy);
                        }
                    }
                }
                if (Program.Ignite != null && Program.Ignite.IsReady() && Program.SubMenu["Combo"]["Ignite"].Cast<CheckBox>().CurrentValue)
                {
                    if (TsTarget.Distance(Program.myHero) <= (600) && Program.myHero.GetSummonerSpellDamage(TsTarget, DamageLibrary.SummonerSpells.Ignite) >= TsTarget.Health)
                    {
                        Program.Ignite.Cast(TsTarget);
                    }
                }
            }
            if (Program.R.IsReady() && Program.SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue)
            {
                List<AIHeroClient> enemies = EntityManager.Heroes.Enemies;
                foreach (AIHeroClient enemy in enemies)
                {
                    if (Program.myHero.Distance(enemy) <= 1200)
                    {
                        var enemiesKnockedUp =
                            ObjectManager.Get<AIHeroClient>()
                            .Where(x => x.IsValidTarget(Program.R.Range))
                            .Where(x => x.HasBuffOfType(BuffType.Knockup));

                        var enemiesKnocked = enemiesKnockedUp as IList<AIHeroClient> ?? enemiesKnockedUp.ToList();
                        if (enemy.IsValidTarget(Program.R.Range) && Program.SubMenu["Combo"][TsTarget.ChampionName].Cast<CheckBox>().CurrentValue && Program.CanCastDelayR(enemy) && enemiesKnocked.Count() >= (Program.SubMenu["Combo"]["R3"].Cast<Slider>().CurrentValue))
                        {
                            Program.R.Cast();
                        }
                    }
                    if (enemy.IsValidTarget(Program.R.Range))
                    {
                        if (Program.IsKnockedUp(enemy) && Program.CanCastDelayR(enemy) && (enemy.Health / enemy.MaxHealth * 100 <= (Program.SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)))
                        {
                            Program.R.Cast();
                        }
                        else if (Program.IsKnockedUp(enemy) && Program.SubMenu["Combo"][TsTarget.ChampionName].Cast<CheckBox>().CurrentValue && Program.CanCastDelayR(enemy) && enemy.HealthPercent >= (Program.SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue) && (Program.SubMenu["Combo"]["R4"].Cast<CheckBox>().CurrentValue))
                        {
                            if (Program.AlliesNearTarget(TsTarget, 600))
                            {
                                Program.R.Cast();
                            }
                        }
                    }
                }
            }
        }

        public static void Flee()
        {
            foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myHero.ServerPosition, Program.E.Range, true))
            {
                var bestMinion =
                   ObjectManager.Get<Obj_AI_Base>()
                       .Where(x => x.IsValidTarget(Program.E.Range))
                       .Where(x => x.Distance(Game.CursorPos) < Program.myHero.Distance(Game.CursorPos))
                       .OrderByDescending(x => x.Distance(Program.myHero))
                       .FirstOrDefault();

                if (bestMinion != null && Program.myHero.IsFacing(bestMinion) && Program.CanCastE(bestMinion) && (Program.E.IsReady() && Program.SubMenu["Misc"]["EscE"].Cast<CheckBox>().CurrentValue))
                {
                    Program.E.Cast(bestMinion);
                }
                if (Program.SteelTempest.IsReady() && Program.SubMenu["Misc"]["EscQ"].Cast<CheckBox>().CurrentValue)
                {
                    if (!Program.Q3READY(Program.myHero) && Program.SteelTempest.Range == 475)
                    {
                        Program.SteelTempest.Cast(minion);
                    }
                }
            }
            foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myHero.ServerPosition, Program.E.Range, true))
            {
                var bestMinion =
                   EntityManager.MinionsAndMonsters.GetJungleMonsters()
                       .Where(x => x.IsValidTarget(Program.E.Range))
                       .Where(x => x.Distance(Game.CursorPos) < Program.myHero.Distance(Game.CursorPos))
                       .OrderByDescending(x => x.Distance(Program.myHero))
                       .FirstOrDefault();

                if (bestMinion != null && Program.myHero.IsFacing(bestMinion) && Program.CanCastE(minion) && (Program.E.IsReady() && Program.SubMenu["Misc"]["EscE"].Cast<CheckBox>().CurrentValue) && Program.myHero.IsFacing(bestMinion))
                {
                    Program.E.Cast(bestMinion);
                }
                if (Program.SteelTempest.IsReady() && Program.SubMenu["Misc"]["EscQ"].Cast<CheckBox>().CurrentValue)
                {
                    if (!Program.Q3READY(Program.myHero) && Program.SteelTempest.Range == 475)
                    {
                        Program.SteelTempest.Cast(minion);
                    }
                }
            }
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                var bestMinion =
                   ObjectManager.Get<AIHeroClient>()
                       .Where(x => x.IsValidTarget(Program.E.Range))
                       .Where(x => x.Distance(Game.CursorPos) < Program.myHero.Distance(Game.CursorPos))
                       .OrderByDescending(x => x.Distance(Program.myHero))
                       .FirstOrDefault();

                if (bestMinion != null && Program.myHero.IsFacing(bestMinion) && (Program.E.IsReady() && Program.SubMenu["Misc"]["EscE"].Cast<CheckBox>().CurrentValue) && Program.myHero.IsFacing(bestMinion))
                {
                    Program.E.Cast(bestMinion);
                }
            }
        }

        public static void Harass()
        {
            var bestMinion =
                  ObjectManager.Get<Obj_AI_Minion>()
                      .Where(x => x.IsValidTarget(Program.E.Range))
                      .Where(x => x.Distance(Game.CursorPos) < Program.myHero.Distance(Game.CursorPos))
                      .OrderByDescending(x => x.Distance(Program.myHero))
                      .FirstOrDefault();

            if (bestMinion != null && Program.myHero.IsFacing(bestMinion) && Program.CanCastE(bestMinion) && (Program.E.IsReady() && Program.SubMenu["Harass"]["E"].Cast<CheckBox>().CurrentValue))
            {
                Program.E.Cast(bestMinion);
            }
            var TsTarget = TargetSelector2.GetTarget(1300, DamageType.Physical);
            Program.orbwalker.ForceTarget(TsTarget);

            if (TsTarget == null)
            {
                return;
            }

            if (TsTarget != null)
            {
                if (Program.SteelTempest.IsReady() && Program.SubMenu["Harass"]["Q3"].Cast<CheckBox>().CurrentValue)
                {
                    PredictionResult QPred = Program.SteelTempest.GetPrediction(TsTarget);
                    if (!Program.isDashing())
                    {
                        Program.SteelTempest.Cast(QPred.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    }
                    else if (Program.Q3READY(Program.myHero) && Program.isDashing() && Program.myHero.Distance(TsTarget) <= 250 * 250)
                    {
                        Program.SteelTempest.Cast(QPred.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                    }
                }
                if (TsTarget == null)
                {
                    return;
                }
                PredictionResult QPred2 = Program.SteelTempest.GetPrediction(TsTarget);
                if (!Program.Q3READY(Program.myHero) && Program.SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue)
                {
                    Program.SteelTempest.Cast(QPred2.CastPosition); Core.DelayAction(Orbwalking.ResetAutoAttackTimer, 250);
                }
            }
        }

        public static void LastHit()
        {
            foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myHero.ServerPosition, Program.SteelTempest.Range, true).OrderByDescending(m => m.Health))
            {
                if (minion == null)
                {
                    return;
                }

                if (!minion.IsDead && minion != null && Program.SubMenu["LastHit"]["Q"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady() && minion.IsValidTarget() && !Program.Q3READY(Program.myHero))
                {
                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                }
                if (!minion.IsDead && minion != null && Program.SubMenu["LastHit"]["Q3"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady() && minion.IsValidTarget() && Program.Q3READY(Program.myHero))
                {
                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                }
                if (Program.SubMenu["LastHit"]["E"].Cast<CheckBox>().CurrentValue && Program.E.IsReady() && minion.IsValidTarget())
                {
                    if (!Program.UnderEnemyTower(Program.PosAfterE(minion)))
                    {
                        var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                        if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.E))
                        {
                            Program.E.Cast(minion);
                        }
                    }
                }
            }
        }

        public static void LaneClear()
        {
            foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myHero.Position, Program.SteelTempest.Range, true).OrderByDescending(m => m.Health))
            {
                if (minion == null)
                {
                    return;
                }

                if (!minion.IsDead && minion != null && Program.SubMenu["LaneClear"]["Q"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady() && minion.IsValidTarget() && !Program.Q3READY(Program.myHero))
                {
                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                    else if (!Program.Q3READY(Program.myHero))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                }
                if (!minion.IsDead && minion != null && Program.SubMenu["LaneClear"]["Q3"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady() && minion.IsValidTarget() && Program.Q3READY(Program.myHero))
                {
                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                    else if (Program.Q3READY(Program.myHero))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                }
            }
            var allMinionsE = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Program.myHero.Position, Program.E.Range, true);
            foreach (var minion in allMinionsE.Where(Program.CanCastE))
            {
                if (Program.SubMenu["LaneClear"]["E"].Cast<CheckBox>().CurrentValue && Program.E.IsReady() && minion.IsValidTarget(Program.E.Range))
                {
                    if (!Program.UnderEnemyTower(Program.PosAfterE(minion)))
                    {
                        var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                        if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.E))
                        {
                            Program.E.Cast(minion);
                        }
                    }
                }
                if (Program.SubMenu["LaneClear"]["Items"].Cast<CheckBox>().CurrentValue)
                {
                    Program.UseItems(minion);
                }
            }
        }

        public static void JungleClear()
        {
            foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.GetJungleMonsters(Program.myHero.ServerPosition, Program.SteelTempest.Range, true))
            {
                if (minion == null)
                {
                    return;
                }

                if (Program.SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady() && minion.IsValidTarget())
                {

                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                    else if (Program.SteelTempest.IsReady())
                    {
                        Program.SteelTempest.Cast(minion.ServerPosition);
                    }
                }
                if (Program.SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue && Program.E.IsReady() && minion.IsValidTarget(Program.E.Range) && Program.CanCastE(minion))
                {
                    var predHealth = Prediction.Health.GetPrediction(minion, (int)(Program.myHero.Distance(minion.Position) * 1000 / 2000));
                    if (predHealth <= Program.myHero.GetSpellDamage(minion, SpellSlot.E))
                    {
                        Program.E.Cast(minion);
                    }
                    else
                    {
                        Program.E.Cast(minion);
                    }
                }
                if (Program.SubMenu["JungleClear"]["Items"].Cast<CheckBox>().CurrentValue)
                {
                    Program.UseItems(minion);
                }
            }
        }

        public static void KillSteal()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget(Program.SteelTempest.Range))
                {
                    if (Program.SubMenu["Misc"]["KsQ"].Cast<CheckBox>().CurrentValue && Program.SteelTempest.IsReady())
                    {
                        var predHealth = Prediction.Health.GetPrediction(enemy, (int)(Program.myHero.Distance(enemy.Position) * 1000 / 2000));
                        if (predHealth <= Program.myHero.GetSpellDamage(enemy, SpellSlot.Q))
                        {
                            Program.SteelTempest.Cast(enemy.ServerPosition);
                        }
                    }
                    if (!Program.SteelTempest.IsReady() && Program.E.IsReady() && Program.SubMenu["Misc"]["KsE"].Cast<CheckBox>().CurrentValue && (Program.myHero.GetSpellDamage(enemy, SpellSlot.E) >= enemy.Health) && Program.CanCastE(enemy))
                    {
                        var predHealth = Prediction.Health.GetPrediction(enemy, (int)(Program.myHero.Distance(enemy.Position) * 1000 / 2000));
                        if (predHealth <= Program.myHero.GetSpellDamage(enemy, SpellSlot.E))
                        {
                            Program.E.Cast(enemy);
                        }
                    }
                    if (Program.Ignite != null && Program.SubMenu["Misc"]["KsIgnite"].Cast<CheckBox>().CurrentValue && Program.Ignite.IsReady())
                    {
                        var predHealth = Prediction.Health.GetPrediction(enemy, (int)(Program.myHero.Distance(enemy.Position) * 1000 / 2000));
                        if (predHealth <= Program.myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite))
                        {
                            Program.Ignite.Cast(enemy);
                        }
                    }
                }
            }
        }

        public static void sChoose()
        {
            var style = Program.SubMenu["Misc"]["sID"].DisplayName;

            switch (style)
            {
                case "Classic":
                    Player.SetSkinId(0);
                    break;
                case "High-Noon Yasuo":
                    Player.SetSkinId(1);
                    break;
                case "Project Yasuo":
                    Player.SetSkinId(2);
                    break;
            }
        }
    }
}