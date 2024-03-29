﻿using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Core;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.Extensions.SharpDX;
using LeagueSharp.SDK.Core.Utils;
using SharpDX;
using VayneHunter_Reborn_SDK.Utility;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
namespace VayneHunter_Reborn_SDK.MapPosition
{
    class PositionalHelper
    {
        private const float Range = 1200f;
        private const float RangeOffsetAlly = -50f;
        private const float RangeOffsetEnemy = 20f;
        private static Team StrongerTeam
        {
            get
            {
                var enemyCs = GameObjects.EnemyHeroes.Where(m => m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false)).Aggregate(0f, (current, enemy) => current + (enemy.MinionsKilled + enemy.NeutralMinionsKilled));
                var allyCs = GameObjects.AllyHeroes.Where(m => m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false)).Aggregate(0f, (current, ally) => current + (ally.MinionsKilled + ally.NeutralMinionsKilled));
                var allyKills = GameObjects.AllyHeroes.Where(m => m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false)).Aggregate(0f, (current, ally) => current + (ally.ChampionsKilled));
                var enemyKills = GameObjects.EnemyHeroes.Where(m => m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false)).Aggregate(0f, (current, enemy) => current + (enemy.ChampionsKilled));
                return allyCs*17.5f + allyKills*300f >= enemyCs*17.5f + enemyKills*300f ? Team.Ally : Team.Enemy;
               // return Team.Ally;
            }
        }

        private static IEnumerable<Obj_AI_Hero> AlliesClose
        {
            get
            {
                return
                    GameObjects.AllyHeroes.Where(
                        m =>
                            m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false) &&
                            m.CountAlliesInRange(m.IsMelee() ? m.AttackRange * 1.5f : m.AttackRange + RangeOffsetAlly * 1.5f) > 0);
            }
        }

        private static IEnumerable<Obj_AI_Hero> EnemiesClose
        {
            get
            {
                return
                    GameObjects.EnemyHeroes.Where(
                        m =>
                            m.Distance(ObjectManager.Player) <= Range && m.IsValidTarget(Range, false) &&
                            m.CountEnemiesInRange(m.IsMelee()?m.AttackRange*1.5f:m.AttackRange + RangeOffsetEnemy*1.5f) > 0);
            }
        }

        public static IEnumerable<Obj_AI_Hero> MeleeEnemiesTowardsMe
        {
            get
            {
                return
                    GameObjects.EnemyHeroes.Where(
                        m => m.IsMelee() && m.Distance(ObjectManager.Player) <= GetRealAutoAttackRange(m,ObjectManager.Player) 
                            && (m.ServerPosition.ToVector2() + (m.BoundingRadius+25f) * m.Direction.ToVector2().Perpendicular()).Distance(ObjectManager.Player.ServerPosition.ToVector2()) <= m.ServerPosition.Distance(ObjectManager.Player.ServerPosition) 
                            && m.IsValidTarget(Range, false));
            }
        }
        public static float GetRealAutoAttackRange(Obj_AI_Hero Attacker, AttackableUnit target)
        {
            var result = Attacker.AttackRange + Attacker.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        public static void DrawMyZone()
        {
            var currentPath = MyPoints().Select(v2 => new IntPoint(v2.X, v2.Y)).ToList();
            var currentPoly = currentPath.ToPolygon();
            currentPoly.Draw(System.Drawing.Color.Red);
        }
        public static void DrawSafeZone()
        {
            var currentPath = GetSafeZone().Select(v2 => new IntPoint(v2.X, v2.Y)).ToList();
            var currentPoly = currentPath.ToPolygon();
            currentPoly.Draw(System.Drawing.Color.Blue);
        }
        public static void DrawAllyZone()
        {
            var currentPath = GetAllyPoints().Select(v2 => new IntPoint(v2.X, v2.Y)).ToList();
            var currentPoly = currentPath.ToPolygon();
            currentPoly.Draw(System.Drawing.Color.Orange);
        }
        public static void DrawEnemyZone()
        {
            var currentPath = GetEnemyPoints().Select(v2 => new IntPoint(v2.X, v2.Y)).ToList();
            var currentPoly = currentPath.ToPolygon();
            currentPoly.Draw(System.Drawing.Color.White);
        }

        public static void DrawIntersection()
        {
            var allyList = GetAllyPoints();
            var enemyList = GetEnemyPoints();
            var intersectionList = allyList.Intersect(enemyList);
            if (intersectionList.Any())
            {
               //Console.WriteLine("Hiiiii");
            }
            else
            {
              // Console.WriteLine("Not hiii");
            }

            var currentPath = intersectionList.Select(v2 => new IntPoint(v2.X, v2.Y)).ToList();
            var currentPoly = currentPath.ToPolygon();
            currentPoly.Draw(System.Drawing.Color.Violet);
        }
        public static List<Vector2> GetSafeZone()
        {
            var allyList = GetAllyPoints();
            var enemyList = GetEnemyPoints();
            var intersectionList = enemyList.Intersect(allyList);
            //if (intersectionList.Any())
           // {
           //     Console.WriteLine("Hi, I' intersecting");
           // }
           // if(ContainsAllItems(allyList,MyPoints()))
           // {
           //     return StrongerTeam == Team.Ally ? allyList.Concat(intersectionList).ToList() : allyList.Concat(MyPoints()).Except(intersectionList).ToList();
           // }
            return StrongerTeam == Team.Ally ? allyList.ToList() : allyList.Except(intersectionList).ToList();
        }
        public static bool ContainsAllItems(List<Vector2> a, List<Vector2> b)
        {
            return !b.Except(a).Any();
        }
        public static List<Vector2> MyPoints()
        {
            List<Geometry.Polygon> polygonsList = new List<Geometry.Polygon>();
            {
                polygonsList.Add(
                    new Geometry.Circle(
                        ObjectManager.Player.ServerPosition.ToVector2(), ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius + RangeOffsetAlly)
                        .ToPolygon());
            }
            var pathList = Geometry.ClipPolygons(polygonsList);
            var pointList = pathList.SelectMany(path => path, (path, point) => new Vector2(point.X, point.Y)).Where(currentPoint => !currentPoint.IsWall() && !Geometry.IsOverWall(ObjectManager.Player.ServerPosition,currentPoint.ToVector3())).ToList();
            return pointList;
        }
        public static List<Vector2> GetAllyPoints()
        {
            var polygonsList = AlliesClose.Select(ally => new Geometry.Circle(ally.ServerPosition.ToVector2(), (ally.IsMeele?ally.AttackRange*1.5f:ally.AttackRange) + ally.BoundingRadius + RangeOffsetAlly).ToPolygon()).ToList();
            var pathList = Geometry.ClipPolygons(polygonsList);
            var pointList = pathList.SelectMany(path => path, (path, point) => new Vector2(point.X, point.Y)).Where(currentPoint => !currentPoint.IsWall()).ToList();
            return pointList;
        }
        public static List<Vector2> GetEnemyPoints()
        {
            var polygonsList = EnemiesClose.Select(enemy => new Geometry.Circle(enemy.ServerPosition.ToVector2(), (enemy.IsMeele ? enemy.AttackRange * 1.5f : enemy.AttackRange) + enemy.BoundingRadius + RangeOffsetEnemy).ToPolygon()).ToList();
            var pathList = Geometry.ClipPolygons(polygonsList);
            var pointList = pathList.SelectMany(path => path, (path, point) => new Vector2(point.X, point.Y)).Where(currentPoint => !currentPoint.IsWall()).ToList();
            return pointList;
        }

    }

    enum Team
    {
        Ally,Enemy
    }
}
