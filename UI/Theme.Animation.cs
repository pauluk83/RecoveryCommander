using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RecoveryCommander.UI
{
    public static partial class Theme
    {
        #region UI Animations
        public static class Animations
        {
            private const int ANIMATION_DURATION = 300;

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool AnimateWindow(IntPtr hwnd, uint dwTime, uint dwFlags);

            private const int AW_HOR_POSITIVE = 0x00000001;
            private const int AW_HOR_NEGATIVE = 0x00000002;
            private const int AW_CENTER = 0x00000010;
            private const int AW_HIDE = 0x00010000;
            private const int AW_ACTIVATE = 0x00020000;
            private const int AW_SLIDE = 0x00040000;

            public static async Task FadeInAsync(Control control, int duration = ANIMATION_DURATION)
            {
                if (control is Form form)
                {
                    form.Opacity = 0;
                    form.Visible = true;
                    for (int i = 0; i <= 10; i++)
                    {
                        form.Opacity = i / 10.0;
                        await Task.Delay(duration / 10);
                    }
                }
                else if (control != null) control.Visible = true;
            }

            public static async Task FadeOutAsync(Control control, int duration = ANIMATION_DURATION)
            {
                if (control is Form form)
                {
                    for (int i = 10; i >= 0; i--)
                    {
                        form.Opacity = i / 10.0;
                        await Task.Delay(duration / 10);
                    }
                    form.Visible = false;
                }
                else if (control != null) control.Visible = false;
            }

            public static void SlideInFromLeft(Form form) => AnimateWindow(form.Handle, (uint)ANIMATION_DURATION, AW_SLIDE | AW_HOR_POSITIVE | AW_ACTIVATE);
            public static void SlideOutToRight(Form form) => AnimateWindow(form.Handle, (uint)ANIMATION_DURATION, AW_SLIDE | AW_HOR_NEGATIVE | AW_HIDE);
            public static void CenterShow(Form form) => AnimateWindow(form.Handle, (uint)ANIMATION_DURATION, AW_CENTER | AW_ACTIVATE);
        }
        #endregion

        #region UI Animation System
        public static class Animator
        {
            private static readonly List<AnimationInstance> ActiveAnimations = new();
            private static readonly System.Windows.Forms.Timer AnimationTimer;

            static Animator()
            {
                AnimationTimer = new System.Windows.Forms.Timer { Interval = 16 };
                AnimationTimer.Tick += (s, e) => ProcessAnimations();
                AnimationTimer.Start();
            }

            public static void Animate(Control control, string property, float targetValue, int durationMs = 250, EasingFunction easing = EasingFunction.CubicOut)
            {
                if (control == null) return;
                var anim = new AnimationInstance {
                    Target = control, PropertyName = property,
                    StartValue = GetPropertyValue(control, property), TargetValue = targetValue,
                    StartTime = DateTime.Now, Duration = durationMs, Easing = easing
                };
                lock (ActiveAnimations) {
                    ActiveAnimations.RemoveAll(a => a.Target == control && a.PropertyName == property);
                    ActiveAnimations.Add(anim);
                }
            }

            private static float GetPropertyValue(Control control, string property) => property switch {
                "Opacity" => (control is Form f) ? (float)f.Opacity : 1f,
                "Left" => control.Left, "Top" => control.Top, "Width" => control.Width, "Height" => control.Height,
                _ => 0f
            };

            private static void SetPropertyValue(Control control, string property, float value) {
                switch (property) {
                    case "Opacity": if (control is Form f) f.Opacity = value; break;
                    case "Left": control.Left = (int)value; break;
                    case "Top": control.Top = (int)value; break;
                    case "Width": control.Width = (int)value; break;
                    case "Height": control.Height = (int)value; break;
                }
            }

            private static void ProcessAnimations() {
                lock (ActiveAnimations) {
                    var now = DateTime.Now;
                    for (int i = ActiveAnimations.Count - 1; i >= 0; i--) {
                        var anim = ActiveAnimations[i];
                        float progress = Math.Min(1f, (float)(now - anim.StartTime).TotalMilliseconds / anim.Duration);
                        float easedProgress = ApplyEasing(progress, anim.Easing);
                        float currentValue = anim.StartValue + (anim.TargetValue - anim.StartValue) * easedProgress;
                        if (anim.Target.IsDisposed) { ActiveAnimations.RemoveAt(i); continue; }
                        if (anim.Target.InvokeRequired) anim.Target.BeginInvoke(new Action(() => SetPropertyValue(anim.Target, anim.PropertyName, currentValue)));
                        else SetPropertyValue(anim.Target, anim.PropertyName, currentValue);
                        if (progress >= 1f) ActiveAnimations.RemoveAt(i);
                    }
                }
            }

            private static float ApplyEasing(float t, EasingFunction easing) => easing switch {
                EasingFunction.CubicOut => 1f - (float)Math.Pow(1f - t, 3),
                EasingFunction.QuickOut => 1f - (float)Math.Pow(1f - t, 4),
                _ => t
            };

            public enum EasingFunction { Linear, CubicOut, QuickOut }
            private class AnimationInstance {
                public Control Target = null!; public string PropertyName = "";
                public float StartValue; public float TargetValue;
                public DateTime StartTime; public int Duration; public EasingFunction Easing;
            }
        }
        #endregion
    }
}
