/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuestListRepair.Core;

namespace QuestListRepair;

static class Ui
{
    public static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    public static bool Animate => SystemParameters.ClientAreaAnimation;

    static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

    static DoubleAnimation Tween(double from, double to, int ms) => new(from, to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = EaseOut };

    public static void ShowPage(Panel pages, FrameworkElement page, int direction = 0)
    {
        foreach (FrameworkElement child in pages.Children)
            if (child != page) child.Visibility = Visibility.Collapsed;
        if (page.Visibility == Visibility.Visible) return;
        page.Visibility = Visibility.Visible;
        if (!Animate) return;
        var move = new TranslateTransform();
        page.RenderTransform = move;
        move.BeginAnimation(TranslateTransform.YProperty, Tween(direction == 0 ? 12 : direction * 28, 0, 260));
        page.BeginAnimation(UIElement.OpacityProperty, Tween(0, 1, 220));
    }

    static int dialogVersion;

    public static void ShowDialog(FrameworkElement layer, FrameworkElement card)
    {
        dialogVersion++;
        layer.Visibility = Visibility.Visible;
        if (!Animate) return;
        var scale = new ScaleTransform();
        card.RenderTransformOrigin = new Point(0.5, 0.5);
        card.RenderTransform = scale;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, Tween(0.94, 1, 240));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Tween(0.94, 1, 240));
        layer.BeginAnimation(UIElement.OpacityProperty, Tween(0, 1, 180));
    }

    public static void HideDialog(FrameworkElement layer)
    {
        if (!Animate)
        {
            layer.Visibility = Visibility.Collapsed;
            return;
        }
        var version = dialogVersion;
        var fade = Tween(layer.Opacity, 0, 140);
        fade.Completed += (_, _) =>
        {
            if (version == dialogVersion) layer.Visibility = Visibility.Collapsed;
        };
        layer.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    public static void Spin(RotateTransform rotate, bool on)
    {
        if (!Animate) return;
        if (on)
        {
            rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(800)) { RepeatBehavior = RepeatBehavior.Forever });
            return;
        }
        var angle = rotate.Angle;
        var stop = Tween(angle, 360, 200 + (int)((360 - angle) / 360 * 600));
        stop.FillBehavior = FillBehavior.Stop;
        rotate.BeginAnimation(RotateTransform.AngleProperty, stop);
    }

    public static void Pulse(UIElement element)
    {
        if (Animate) element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0.45, TimeSpan.FromMilliseconds(600)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
    }

    public static void Reveal(FrameworkElement element)
    {
        if (!Animate) return;
        var scale = new ScaleTransform();
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = scale;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, Tween(0.85, 1, 220));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Tween(0.85, 1, 220));
        element.BeginAnimation(UIElement.OpacityProperty, Tween(0, 1, 220));
    }

    public static void AnimateOrbs(TranslateTransform move1, ScaleTransform scale1, TranslateTransform move2, ScaleTransform scale2)
    {
        if (!SystemParameters.ClientAreaAnimation) return;
        void Drift(Animatable target, DependencyProperty property, double from, double to, double seconds)
        {
            var animation = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            target.BeginAnimation(property, animation);
        }
        Drift(move1, TranslateTransform.XProperty, 0, -90, 24);
        Drift(move1, TranslateTransform.YProperty, 0, 70, 24);
        Drift(scale1, ScaleTransform.ScaleXProperty, 1, 1.18, 24);
        Drift(scale1, ScaleTransform.ScaleYProperty, 1, 1.18, 24);
        Drift(move2, TranslateTransform.XProperty, 110, 0, 31);
        Drift(move2, TranslateTransform.YProperty, -60, 0, 31);
        Drift(scale2, ScaleTransform.ScaleXProperty, 1.15, 1, 31);
        Drift(scale2, ScaleTransform.ScaleYProperty, 1.15, 1, 31);
    }

    public static void RenderSteps(Panel list, Operation operation)
    {
        list.Children.Clear();
        foreach (var step in operation.Steps)
        {
            var dock = new DockPanel { Margin = new Thickness(16, 10, 16, 10) };
            var marker = new Grid { Width = 24, Height = 24 };
            switch (step.State)
            {
                case StepState.Done:
                case StepState.Skipped:
                    marker.Children.Add(new Border { CornerRadius = new CornerRadius(12), Background = step.State == StepState.Done ? Res("SuccessSoft") : Res("Elevated") });
                    marker.Children.Add(Icons.Make(Icons.Check, step.State == StepState.Done ? Res("Success") : Res("Text2"), 14, 3));
                    break;
                case StepState.Running:
                    marker.Children.Add(new Border { CornerRadius = new CornerRadius(12), BorderBrush = Res("AccentFg"), BorderThickness = new Thickness(2) });
                    marker.Children.Add(new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Background = Res("AccentFg") });
                    break;
                case StepState.Failed:
                    marker.Children.Add(new Border { CornerRadius = new CornerRadius(12), Background = Res("AccentSoft") });
                    marker.Children.Add(Icons.Make(Icons.Cross, Res("AccentFg"), 14, 3));
                    break;
                default:
                    marker.Children.Add(new Border { CornerRadius = new CornerRadius(12), BorderBrush = Res("Line"), BorderThickness = new Thickness(2) });
                    break;
            }
            DockPanel.SetDock(marker, Dock.Left);
            dock.Children.Add(marker);
            dock.Children.Add(new TextBlock
            {
                Text = step.Title,
                Style = (Style)Application.Current.Resources["Item"],
                Foreground = step.State == StepState.Pending ? Res("Text2") : Res("Text"),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            list.Children.Add(dock);
        }
    }
}
