/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QuestListRepair;

static class Icons
{
    public const string Monitor = "M5 4 H19 A2 2 0 0 1 21 6 V14 A2 2 0 0 1 19 16 H5 A2 2 0 0 1 3 14 V6 A2 2 0 0 1 5 4 Z M8 20 H16 M12 16 V20";
    public const string Layers = "M12 3 L21 8 L12 13 L3 8 Z M3 13 L12 18 L21 13";
    public const string Refresh = "M20 11 A8 8 0 1 0 17.7 16.7 M20 4 V11 H13";
    public const string List = "M9 6 H20 M9 12 H20 M9 18 H20 M4.5 6 L4.6 6 M4.5 12 L4.6 12 M4.5 18 L4.6 18";
    public const string Zap = "M13 2 L4 14 H11 L10 22 L19 10 H12 Z";
    public const string Wrench = "M14.7 6.3 A4 4 0 0 0 9.3 11.7 L3 18 L6 21 L12.3 14.7 A4 4 0 0 0 17.7 9.3 L15.1 11.9 L12.7 11.3 L12.1 8.9 Z";
    public const string Undo = "M9 14 L4 9 L9 4 M4 9 H15 A5 5 0 0 1 15 19 H12";
    public const string Trash = "M4 7 H20 M10 11 V17 M14 11 V17 M6 7 L7 20 H17 L18 7 M9 7 V4 H15 V7";
    public const string Archive = "M4 4 H20 A1 1 0 0 1 21 5 V8 A1 1 0 0 1 20 9 H4 A1 1 0 0 1 3 8 V5 A1 1 0 0 1 4 4 Z M5 9 V19 H19 V9 M10 13 H14";
    public const string Pulse = "M3 12 H7 L10 4 L14 20 L17 12 H21";
    public const string Doc = "M6 3 H15 L19 7 V21 H6 Z M9 12 H16 M9 16 H16 M9 8 H12";
    public const string Info = "M3 12 A9 9 0 1 0 21 12 A9 9 0 1 0 3 12 Z M12 11 V17 M12 7.5 L12.1 7.5";
    public const string Alert = "M12 3 L22 21 H2 Z M12 10 V14 M12 17 L12.1 17";
    public const string Chevron = "M9 6 L15 12 L9 18";
    public const string Check = "M5 12 L10 17 L19 7";
    public const string Cross = "M6 6 L18 18 M18 6 L6 18";
    public const string Plus = "M12 5 V19 M5 12 H19";

    public static Viewbox Make(string data, Brush brush, double size = 18, double stroke = 1.8)
    {
        var path = new Path
        {
            Data = Geometry.Parse(data),
            Stroke = brush,
            StrokeThickness = stroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
        };
        var canvas = new Canvas { Width = 24, Height = 24 };
        canvas.Children.Add(path);
        return new Viewbox { Width = size, Height = size, Child = canvas };
    }
}
