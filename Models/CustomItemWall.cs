using UnityEngine;

namespace ImprovePlayable
{
    public class CustomItemWall : CItem_Wall
    {
        public CSurface m_surface;

        public CustomItemWall(CTile tileWorld, CTile tileIcon, ushort hpMax, uint colorMap, int forceResist, float weight, Type type, CSurface surface) 
            : base(tileWorld, tileIcon, hpMax, colorMap, forceResist, weight, type)
        {
            this.m_surface = surface;
        }
    }
}