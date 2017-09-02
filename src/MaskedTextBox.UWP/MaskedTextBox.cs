using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace HoveyTech.MaskedTextbox.UWP
{
    /// <summary>
    /// 
    /// </summary>
    public class MaskedTextBox : TextBox
    {
        private bool _advanceSelectionStartFromKeyDown;

        /// <summary>
        /// 
        /// </summary>
        public MaskedTextBox()
        {
            this.KeyDown += OnKeyDown;
            this.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            ValidateSelection();
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
        {
            var key = keyRoutedEventArgs.Key;

            if (key == VirtualKey.Tab) return;

            keyRoutedEventArgs.Handled = true;

            var realIndex = GetIndexOfRealTextFromSelectionStart();

            if (realIndex == -1)
                return;

            if (key == VirtualKey.Back)
            {
                var actualSelect = SelectionStart + 1;
                var nextSelectionStart = actualSelect - 1;
                var realText = GetRealTextSafe();

                if (realIndex > realText.Length - 1)
                    realIndex = realText.Length - 1;

                if (realIndex == -1) return;

                RealText = GetRealTextSafe().Remove(realIndex, 1);
                SelectionStart = nextSelectionStart;
            }
            else if (key == VirtualKey.Left)
            {
                if (realIndex <= 0) return;
                SelectionStart = realIndex - 1;
            }
            else if (key == VirtualKey.Right)
            {
                var realText = GetRealTextSafe();
                if (realIndex >= realText.Length) return;

                SelectionStart = realIndex + 1;
            }
            else if ((key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
                     || (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9))
            {
                if (RealText != null && RealText.Length >= GetMaskLength())
                    return;

                if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
                    key = (VirtualKey)((int)key - 48);

                var keyd = Convert.ToChar(key);
                var originalSelectionStart = SelectionStart;
                var nextSelectionStart = originalSelectionStart + 1;
                var realText = GetRealTextSafe().Insert(realIndex, keyd.ToString());

                int extraChars = 0;

                while (Mask.Length > nextSelectionStart && !IsUserPlaceholder(Mask[nextSelectionStart]))
                {
                    extraChars++;
                    nextSelectionStart++;
                }

                RealText = realText;
                SelectionStart = originalSelectionStart + extraChars + 1;
                _advanceSelectionStartFromKeyDown = true;
            }
        }

        private string GetRealTextSafe()
        {
            return RealText ?? string.Empty;
        }

        private int GetIndexOfRealTextFromSelectionStart()
        {
            if (string.IsNullOrEmpty(Mask)) return -1;

            int realTextIndex = 0;

            for (int i = 0; i < Mask.Length; i++)
            {
                var maskChar = Mask[i];

                if (!IsUserPlaceholder(maskChar))
                    continue;

                if (string.IsNullOrEmpty(RealText))
                    return realTextIndex;

                if (realTextIndex == RealText.Length)
                    return realTextIndex;

                if (i == SelectionStart)
                    return realTextIndex;

                realTextIndex++;
            }

            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register(
            nameof(Mask), typeof(string), typeof(MaskedTextBox), new PropertyMetadata(null, MaskPropertyChangedCallback));

        /// <summary>
        /// 
        /// </summary>
        public string Mask
        {
            get { return (string)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty RealTextProperty = DependencyProperty.Register(
            nameof(RealText), typeof(string), typeof(MaskedTextBox), new PropertyMetadata(null, RealTextPropertyChangedCallback));

        /// <summary>
        /// 
        /// </summary>
        public string RealText
        {
            get { return (string)GetValue(RealTextProperty); }
            set { SetValue(RealTextProperty, value); }
        }

        private static void RealTextPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (MaskedTextBox)dependencyObject;
            control.Text = control.GetMaskedText();
        }

        private static void MaskPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var control = (MaskedTextBox)dependencyObject;
            control.Text = control.GetMaskedText();
        }

        private bool IsValidCharForMask(char c, char mask)
        {
            switch (mask)
            {
                case '9':
                    int i;
                    return int.TryParse(c.ToString(), out i);
                default:
                    return false;
            }
        }

        private bool IsUserPlaceholder(char mask)
        {
            switch (mask)
            {
                case '9':
                    return true;
                default:
                    return false;
            }
        }

        private int GetMaskLength()
        {
            if (Mask == null) return 0;
            return Mask.Count(IsUserPlaceholder);
        }

        private string GetMaskedText()
        {
            string result = string.Empty;

            if (string.IsNullOrEmpty(Mask)) return result;

            int realTextIndex = 0;
            bool encounteredInvalid = false;

            for (int i = 0; i < Mask.Length; i++)
            {
                var maskChar = Mask[i];

                if (!IsUserPlaceholder(maskChar))
                {
                    result += maskChar;
                    continue;
                }

                if (RealText == null || realTextIndex >= RealText.Length || encounteredInvalid)
                {
                    result += "_";
                    continue;
                }

                var realTextChar = RealText[realTextIndex];
                var validChar = IsValidCharForMask(realTextChar, maskChar);

                if (!validChar)
                {
                    encounteredInvalid = true;
                    result += "_";
                    continue;
                }

                result += realTextChar;

                realTextIndex++;
            }

            return result;
        }

        private void ValidateSelection()
        {
            var validateSelectionStartLocations = new List<int>();

            if (string.IsNullOrEmpty(Mask)) return;

            int realTextIndex = 0;
            int i = 0;
            bool lastMaskCharUserPlaceHolder = false;

            for (; i < Mask.Length; i++)
            {
                var maskChar = Mask[i];

                if (!IsUserPlaceholder(maskChar))
                {
                    if (lastMaskCharUserPlaceHolder)
                        validateSelectionStartLocations.Add(i);

                    lastMaskCharUserPlaceHolder = false;
                    continue;
                }

                lastMaskCharUserPlaceHolder = true;
                validateSelectionStartLocations.Add(i);

                if (string.IsNullOrEmpty(RealText) || realTextIndex >= RealText.Length)
                    break;

                realTextIndex++;
            }

            if (i == Mask.Length && realTextIndex == RealText.Length)
                validateSelectionStartLocations.Add(i);

            if (!validateSelectionStartLocations.Any())
                return;

            int closest = -1;

            if (_advanceSelectionStartFromKeyDown)
            {
                foreach (var selection in validateSelectionStartLocations)
                {
                    if (selection >= SelectionStart)
                    {
                        closest = selection;
                        break;
                    }
                }

                _advanceSelectionStartFromKeyDown = false;

                if (closest == -1)
                    return;
            }
            else
                closest = validateSelectionStartLocations.Aggregate((x, y) => Math.Abs(x - SelectionStart) < Math.Abs(y - SelectionStart) ? x : y);

            if (closest != SelectionStart)
                SelectionStart = closest;
        }


    }
}
