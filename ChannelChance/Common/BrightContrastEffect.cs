using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ChannelChance.Common
{
    public class BrightContrastEffect : ShaderEffect
    {
        public BrightContrastEffect()
        {
            PixelShader = m_shader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(BrightnessProperty);
            UpdateShaderValue(ContrastProperty);

        }

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BrightContrastEffect), 0);

        public double Brightness
        {
            get { return (float)GetValue(BrightnessProperty); }
            set { SetValue(BrightnessProperty, value); }
        }

        public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register("Brightness", typeof(double), typeof(BrightContrastEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        public double Contrast
        {
            get { return (float)GetValue(ContrastProperty); }
            set { SetValue(ContrastProperty, value); }
        }

        public static readonly DependencyProperty ContrastProperty = DependencyProperty.Register("Contrast", typeof(double), typeof(BrightContrastEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        private static PixelShader m_shader = new PixelShader() { UriSource = new Uri(@"pack://application:,,,/ChannelChance;component/bricon.ps") };

    }
}
