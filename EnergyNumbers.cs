using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCEnergy;

internal class EnergyNumbers
{
    public static void Render(int number, double x, double y, Color color)
    {
        string text = DB.IntStringCache(number);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            Rect value = new Rect((c >= '0' && c <= '9') ? (7 * (c - 48)) : 0, 0.0, 7.0, 8.0);
            double num = x + (double)(i * 7);
            Spr? id = ModEntry.Instance.EnergyNumbers.Sprite;
            double x2 = num - 1.0;
            double y2 = y - 1.0;
            Color? color2 = color;
            Rect? pixelRect = value;
            Draw.Sprite(id, x2, y2, flipX: false, flipY: false, 0.0, null, null, null, pixelRect, color2);
        }
    }
}
