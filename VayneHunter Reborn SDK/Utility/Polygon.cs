﻿using System.Collections.Generic;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Core.Extensions.SharpDX;
using SharpDX;

namespace VayneHunter_Reborn_SDK.Utility
{
    internal class Polygon //Polygon Class by DETUKS
    {
        public List<Vector2> Points;

        public Polygon(List<Vector2> p)
        {
            Points = p;
        }

        public void Add(Vector2 vec)
        {
            Points.Add(vec);
        }

        public int Count()
        {
            return Points.Count;
        }

        public bool Contains(Vector2 point)
        {
            var result = false;
            var j = Count() - 1;
            for (var i = 0; i < Count(); i++)
            {
                if (Points[i].Y < point.Y && Points[j].Y >= point.Y || Points[j].Y < point.Y && Points[i].Y >= point.Y)
                {
                    if (Points[i].X +
                        (point.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) * (Points[j].X - Points[i].X) < point.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static List<Vector2> Rectangle(Vector2 startVector2, Vector2 endVector2, float radius)
        {
            var points = new List<Vector2>();

            var v1 = endVector2 - startVector2;
            var to1Side = Vector2.Normalize(v1).Perpendicular() * radius;

            points.Add(startVector2 + to1Side);
            points.Add(startVector2 - to1Side);
            points.Add(endVector2 - to1Side);
            points.Add(endVector2 + to1Side);
            return points;
        }
    }
}
