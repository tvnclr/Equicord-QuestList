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
