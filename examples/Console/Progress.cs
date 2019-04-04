/*
  █▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀  ▀  ▀      ▀▀
  █  The MIT License (MIT)
  █
  █  Copyright (c) 2019 JP Dillingham (jp@dillingham.ws)
  █
  █  Permission is hereby granted, free of charge, to any person obtaining a copy
  █  of this software and associated documentation files (the "Software"), to deal
  █  in the Software without restriction, including without limitation the rights
  █  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  █  copies of the Software, and to permit persons to whom the Software is
  █  furnished to do so, subject to the following conditions:
  █
  █  The above copyright notice and this permission notice shall be included in all
  █  copies or substantial portions of the Software.
  █
  █  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  █  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  █  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  █  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  █  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  █  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  █  SOFTWARE.
  █
  ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀  ▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀██
                                                                                               ██
                                                                                           ▀█▄ ██ ▄█▀
                                                                                             ▀████▀
                                                                                               ▀▀                            */

namespace Console
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Provides utility method(s) for the Progress namespace.
    /// </summary>
    public static class ProgressUtility
    {
        /// <summary>
        ///     Returns a value incidating whether <see cref="Console"/> is available to the current execution context.
        /// </summary>
        /// <returns>A value indicating whether <see cref="Console"/> is available to the current execution context.</returns>
        [ExcludeFromCodeCoverage]
        public static bool ConsoleAvailable()
        {
            try
            {
                return Console.WindowWidth > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     A progress indicator that scrolls a given string.
    /// </summary>
    public class Marquee
    {
        private string internalText;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Marquee"/> class.
        /// </summary>
        /// <param name="text">The text to scroll.</param>
        /// <param name="width">The width of the marquee, in number of characters.</param>
        /// <param name="scrollOnToString">A value indicating whether <see cref="Scroll()"/> is invoked on calls to <see cref="ToString()"/>.</param>
        /// <param name="format">The marquee format.</param>
        public Marquee(string text, int width, bool scrollOnToString = false, MarqueeFormat format = null)
        {
            Text = text;
            Width = width;
            Format = format ?? new MarqueeFormat();
            Position = Text.Length + Width;
            Gap = Format.Gap ?? Width;

            ScrollOnToString = scrollOnToString;

            SetText();
        }

        /// <summary>
        ///     Gets the marquee format.
        /// </summary>
        public MarqueeFormat Format { get; }

        /// <summary>
        ///     Gets the gap between repetitions of the <see cref="Text"/>.
        /// </summary>
        public int Gap { get; }

        /// <summary>
        ///     Gets the current position of the end of the <see cref="Text"/> along the <see cref="Width"/>.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the scrolling direction is reversed from the original direction.
        /// </summary>
        public bool Reversed { get; private set; }

        /// <summary>
        ///     Gets the text to scroll.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        ///     Gets the width of the marquee, in number of characters.
        /// </summary>
        public int Width { get; }

        /// <summary>
        ///     Gets a value indicating whether <see cref="Scroll"/> is invoked on calls to <see cref="ToString"/>.
        /// </summary>
        public bool ScrollOnToString { get; }

        /// <summary>
        ///     Scrolls the marquee one character to the left or right, depending on formatting options.
        /// </summary>
        public void Scroll()
        {
            if (Format.LeftToRight ^ Reversed)
            {
                ScrollRight();
            }
            else
            {
                ScrollLeft();
            }

            if (Position == 0 && Format.Bounce)
            {
                Reversed = !Reversed;

                if (Format.ReverseTextOnBounce)
                {
                    Text = new string(Text.Reverse().ToArray());
                    SetText();
                }
            }
        }

        /// <summary>
        ///     Returns the current display text of the marquee.
        /// </summary>
        /// <returns>The current display text of the marquee.</returns>
        public override string ToString()
        {
            if (Format.HiddenWhen())
            {
                return string.Empty;
            }

            if (Format.EmptyWhen())
            {
                return new string(Format.Empty, Format.Width + Width);
            }

            if (ScrollOnToString)
            {
                Scroll();
            }

            var builder = new StringBuilder();
            builder.Append(new string(Format.Pad, Format.PaddingLeft));
            builder.Append(Format.Left);

            if (Format.CompleteWhen())
            {
                builder.Append(Format.Complete.PadRight(Width, Format.Empty));
            }
            else
            {
                builder.Append(new string(internalText.Take(Width).ToArray()));
            }

            builder.Append(Format.Right);
            builder.Append(new string(Format.Pad, Format.PaddingRight));

            return builder.ToString();
        }

        private void ScrollLeft()
        {
            internalText = new string(internalText.Skip(1).ToArray()) + new string(internalText.Take(1).ToArray());
            Position = --Position % (Text.Length + Width);
        }

        private void ScrollRight()
        {
            internalText = new string(internalText.Skip(internalText.Length - 1).ToArray()) + new string(internalText.Take(internalText.Length - 1).ToArray());
            Position = ++Position % (Text.Length + Width);
        }

        private void SetText()
        {
            var builder = new StringBuilder();

            do
            {
                builder.Append(new string(Format.Empty, Gap));
                builder.Append(Text);
            }
            while (builder.Length < Width);

            internalText = builder.ToString();
        }
    }

    /// <summary>
    ///     Formatting options for marquee displays.
    /// </summary>
    public class MarqueeFormat : ProgressFormat
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MarqueeFormat"/> class.
        /// </summary>
        /// <param name="empty">The character representing empty space.</param>
        /// <param name="complete">The string to display when <paramref name="completeWhen"/> evaluates to true.</param>
        /// <param name="left">The string to prepend to the left side of the display.</param>
        /// <param name="right">The string to append to the right side of the display.</param>
        /// <param name="paddingLeft">
        ///     The amount of padding, in number of characters, to prepend to the left side of the display.
        /// </param>
        /// <param name="paddingRight">
        ///     The amount of padding, in number of characters, to append to the right side of the display.
        /// </param>
        /// <param name="pad">The character used for padding.</param>
        /// <param name="bounce">
        ///     A value indicating whether the scroll direction should reverse when the text scrolls past the display boundary.
        /// </param>
        /// <param name="gap">
        ///     The number of characters of <paramref name="empty"/> space between copies of the text. Defaults to the width of the
        ///     marquee when omitted.
        /// </param>
        /// <param name="leftToRight">A value indicating whether the marquee should scroll from left to right.</param>
        /// <param name="reverseTextOnBounce">A value indicating whether the text should be reversed on bounce.</param>
        /// <param name="completeWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="complete"/> string.
        /// </param>
        /// <param name="emptyWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="empty"/> character.
        /// </param>
        /// <param name="hiddenWhen">
        ///     The function used to determine whether the display should be composed of only a zero-length string.
        /// </param>
        public MarqueeFormat(
            char empty = ' ',
            string complete = null,
            string left = null,
            string right = null,
            int paddingLeft = 0,
            int paddingRight = 0,
            char pad = ' ',
            bool bounce = false,
            bool reverseTextOnBounce = false,
            bool leftToRight = false,
            int? gap = null,
            Func<bool> completeWhen = null,
            Func<bool> emptyWhen = null,
            Func<bool> hiddenWhen = null)
            : base(empty, left, right, paddingLeft, paddingRight, pad, emptyWhen, hiddenWhen)
        {
            Bounce = bounce;
            ReverseTextOnBounce = reverseTextOnBounce;
            LeftToRight = leftToRight;
            Gap = gap;
            Complete = complete;
            CompleteWhen = completeWhen ?? (() => false);
        }

        /// <summary>
        ///     Gets a value indicating whether the scroll direction should reverse when the text scrolls past the display boundary.
        /// </summary>
        public bool Bounce { get; }

        /// <summary>
        ///     Gets the number of characters of <see cref="ProgressFormat.Empty"/> space between copies of the text.
        /// </summary>
        public int? Gap { get; }

        /// <summary>
        ///     Gets a value indicating whether the marquee should scroll from left to right.
        /// </summary>
        public bool LeftToRight { get; }

        /// <summary>
        ///     Gets a value indicating whether the text should be reversed on bounce.
        /// </summary>
        public bool ReverseTextOnBounce { get; }

        /// <summary>
        ///     Gets the function used to determine whether the display should be composed of only the <see cref="Complete"/> string.
        /// </summary>
        public Func<bool> CompleteWhen { get; }

        /// <summary>
        ///     Gets the string to display when <see cref="CompleteWhen"/> evaluates to true.
        /// </summary>
        public string Complete { get; }
    }

    /// <summary>
    ///     A progress indicator that fills a string with characters representing complete and incomplete portions of a given range.
    /// </summary>
    public class ProgressBar
    {
        private int internalValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        /// <param name="width">The width of the progress bar, in characters, with anything less than 1 representing a bar that should be fit to width.</param>
        /// <param name="minimum">The minimum progress value.</param>
        /// <param name="maximum">The maximum progress value.</param>
        /// <param name="step">The amount to add to <see cref="Value"/> on each invocation of <see cref="Step()"/></param>
        /// <param name="value">The current progress value.</param>
        /// <param name="format">The progress bar format.</param>
        public ProgressBar(int width = 0, int minimum = 0, int maximum = 100, int step = 1, int value = 0, ProgressBarFormat format = null)
        {
            if (width <= 0 && !ProgressUtility.ConsoleAvailable())
            {
                throw new ArgumentOutOfRangeException($"Unable to use dynamic width (width < 0) outside of a Console context.  Specify a fixed width.");
            }

            Width = width;
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
            Value = value;
            Format = format ?? new ProgressBarFormat();
        }

        /// <summary>
        ///     Gets a value indicating whether progress is complete.
        /// </summary>
        public bool Complete => Value == Maximum;

        /// <summary>
        ///     Gets the progress bar format.
        /// </summary>
        public ProgressBarFormat Format { get; }

        /// <summary>
        ///     Gets the maximum progress value.
        /// </summary>
        public int Maximum { get; }

        /// <summary>
        ///     Gets the minimum progress value.
        /// </summary>
        public int Minimum { get; }

        /// <summary>
        ///     Gets the current value as a percentage of <see cref="Maximum"/>.
        /// </summary>
        public double Percent => Value / (double)Maximum;

        /// <summary>
        ///     Gets the amount to add to <see cref="Value"/> on each invocation of <see cref="Step()"/>
        /// </summary>
        public int Step { get; }

        /// <summary>
        ///     Gets or sets the current progress value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified value is less than <see cref="Minimum"/> or greater than <see cref="Maximum"/>.</exception>
        public int Value
        {
            get
            {
                return internalValue;
            }

            set
            {
                if (value > Maximum)
                {
                    internalValue = Maximum;
                }
                else if (value < Minimum)
                {
                    internalValue = Minimum;
                }
                else
                {
                    internalValue = value;
                }
            }
        }

        /// <summary>
        ///     Gets the width of the progress bar, in characters, with anything less than 1 representing a bar that should be fit to width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        ///     Decrements <see cref="Value"/> by the specified number of <paramref name="steps"/>.
        /// </summary>
        /// <param name="steps">The number of <see cref="Step"/>s by which to decrement <see cref="Value"/>.</param>
        public void Decrement(int steps = 1)
        {
            Value -= Step * steps;
        }

        /// <summary>
        ///     Increments <see cref="Value"/> by the specified number of <paramref name="steps"/>.
        /// </summary>
        /// <param name="steps">The number of <see cref="Step"/>s by which to decrement <see cref="Value"/>.</param>
        public void Increment(int steps = 1)
        {
            Value += Step * steps;
        }

        /// <summary>
        ///     Returns the current display text of the spinner.
        /// </summary>
        /// <returns>The current display text of the spinner.</returns>
        public override string ToString()
        {
            if (Format.HiddenWhen())
            {
                return string.Empty;
            }

            var barWidth = Width <= 0 ? (Console.WindowWidth - 1) - Format.Width - Math.Abs(Width) : Width;

            if (Format.EmptyWhen())
            {
                return new string(' ', Format.Width + barWidth);
            }

            var percentFull = Value / (double)Maximum;

            var fullChars = (int)Math.Round(barWidth * percentFull, 0);
            var emptyChars = barWidth - fullChars;
            var showTip = false;

            if (fullChars > 0 && fullChars < barWidth)
            {
                showTip = true;
                fullChars--;
            }

            var builder = new StringBuilder();
            builder.Append(new string(Format.Pad, Format.PaddingLeft));
            builder.Append(Format.Left);
            builder.Append(new string(Format.Full, fullChars));

            if (showTip)
            {
                builder.Append(Format.Tip);
            }

            builder.Append(new string(Format.Empty, emptyChars));
            builder.Append(Format.Right);
            builder.Append(new string(Format.Pad, Format.PaddingRight));

            return builder.ToString();
        }
    }

    /// <summary>
    ///     Formatting options for progress bar displays.
    /// </summary>
    public class ProgressBarFormat : ProgressFormat
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProgressBarFormat"/> class.
        /// </summary>
        /// <param name="empty">The character representing empty space.</param>
        /// <param name="full">The character representing 'full' or used space.</param>
        /// <param name="tip">
        ///     The character representing the 'tip' of the progress bar or the transition between full and empty.
        /// </param>
        /// <param name="left">The string to prepend to the left side of the display.</param>
        /// <param name="right">The string to append to the right side of the display.</param>
        /// <param name="paddingLeft">
        ///     The amount of padding, in number of characters, to prepend to the left side of the display.
        /// </param>
        /// <param name="paddingRight">
        ///     The amount of padding, in number of characters, to append to the right side of the display.
        /// </param>
        /// <param name="pad">The character used for padding.</param>
        /// <param name="emptyWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="empty"/> character.
        /// </param>
        /// <param name="hiddenWhen">
        ///     The function used to determine whether the display should be composed of only a zero-length string.
        /// </param>
        public ProgressBarFormat(
            char empty = '░',
            char full = '█',
            char tip = '█',
            string left = null,
            string right = null,
            int paddingLeft = 0,
            int paddingRight = 0,
            char pad = ' ',
            Func<bool> emptyWhen = null,
            Func<bool> hiddenWhen = null)
            : base(empty, left, right, paddingLeft, paddingRight, pad, emptyWhen, hiddenWhen)
        {
            Full = full;
            Tip = tip;
        }

        /// <summary>
        ///     Gets the character representing 'full' or used space.
        /// </summary>
        public char Full { get; }

        /// <summary>
        ///     Gets the character representing the 'tip' of the progress bar or the transition between full and empty.
        /// </summary>
        public char Tip { get; }
    }

    /// <summary>
    ///     Formatting options for progress displays.
    /// </summary>
    public abstract class ProgressFormat
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProgressFormat"/> class.
        /// </summary>
        /// <param name="empty">The character representing empty space.</param>
        /// <param name="left">The string to prepend to the left side of the display.</param>
        /// <param name="right">The string to append to the right side of the display.</param>
        /// <param name="paddingLeft">
        ///     The amount of padding, in number of characters, to prepend to the left side of the display.
        /// </param>
        /// <param name="paddingRight">
        ///     The amount of padding, in number of characters, to append to the right side of the display.
        /// </param>
        /// <param name="pad">The character used for padding.</param>
        /// <param name="emptyWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="empty"/> character.
        /// </param>
        /// <param name="hiddenWhen">
        ///     The function used to determine whether the display should be composed of only a zero-length string.
        /// </param>
        protected ProgressFormat(
            char empty = ' ',
            string left = null,
            string right = null,
            int paddingLeft = 0,
            int paddingRight = 0,
            char pad = ' ',
            Func<bool> emptyWhen = null,
            Func<bool> hiddenWhen = null)
        {
            Empty = empty;
            Left = left;
            Right = right;
            PaddingLeft = paddingLeft;
            PaddingRight = paddingRight;
            Pad = pad;

            EmptyWhen = emptyWhen ?? (() => false);
            HiddenWhen = hiddenWhen ?? (() => false);
        }

        /// <summary>
        ///     Gets the character representing empty space.
        /// </summary>
        public char Empty { get; }

        /// <summary>
        ///     Gets the function used to determine whether the display should be composed of only the <see cref="Empty"/> character.
        /// </summary>
        public Func<bool> EmptyWhen { get; }

        /// <summary>
        ///     Gets the function used to determine whether the display should be composed of only a zero-length string.
        /// </summary>
        public Func<bool> HiddenWhen { get; }

        /// <summary>
        ///     Gets the string to prepend to the left side of the display.
        /// </summary>
        public string Left { get; }

        /// <summary>
        ///     Gets the character used for padding.
        /// </summary>
        public char Pad { get; }

        /// <summary>
        ///     Gets the amount of padding, in number of characters, to prepend to the left side of the display.
        /// </summary>
        public int PaddingLeft { get; }

        /// <summary>
        ///     Gets the amount of padding, in number of characters, to append to the right side of the display.
        /// </summary>
        public int PaddingRight { get; }

        /// <summary>
        ///     Gets the string to append to the right side of the display.
        /// </summary>
        public string Right { get; }

        /// <summary>
        ///     Gets the total width of <see cref="PaddingLeft"/>, <see cref="Left"/>, <see cref="Right"/>, and <see cref="PaddingRight"/>.
        /// </summary>
        public int Width => PaddingLeft + (Left?.Length ?? 0) + (Right?.Length ?? 0) + PaddingRight;
    }

    /// <summary>
    ///     A progress indicator that cycles through a given array of characters.
    /// </summary>
    public class Spinner
    {
        /// <summary>
        ///     The default characters through which to cycle.
        /// </summary>
        public const string DefaultCharacters = "-\\|/";

        /// <summary>
        ///     Initializes a new instance of the <see cref="Spinner"/> class.
        /// </summary>
        /// <param name="frames">The array of characters through which to cycle.</param>
        public Spinner(params char[] frames)
            : this(frames.AsEnumerable())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Spinner"/> class.
        /// </summary>
        /// <param name="frames">A string containing the characters through which to cycle.</param>
        /// <param name="spinOnToString">A value indicating whether <see cref="Spin()"/> is invoked on calls to <see cref="ToString()"/>.</param>
        /// <param name="format">The spinner format.</param>
        public Spinner(string frames = DefaultCharacters, bool spinOnToString = true, SpinnerFormat format = null)
            : this(frames.ToCharArray(), spinOnToString, format)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Spinner"/> class.
        /// </summary>
        /// <param name="frames">The array of characters through which to cycle.</param>
        /// <param name="spinOnToString">A value indicating whether <see cref="Spin()"/> is invoked on calls to <see cref="ToString()"/>.</param>
        /// <param name="format">The spinner format.</param>
        public Spinner(IEnumerable<char> frames, bool spinOnToString = true, SpinnerFormat format = null)
        {
            Frame = 0;
            Frames = frames?.DefaultIfEmpty() ?? DefaultCharacters.ToCharArray();
            SpinOnToString = spinOnToString;
            Format = format ?? new SpinnerFormat();
        }

        /// <summary>
        ///     Gets the spinner format.
        /// </summary>
        public SpinnerFormat Format { get; }

        /// <summary>
        ///     Gets the offset of the current frame.
        /// </summary>
        public int Frame { get; private set; }

        /// <summary>
        ///     Gets the array of characters through which to cycle.
        /// </summary>
        public IEnumerable<char> Frames { get; }

        /// <summary>
        ///     Gets a value indicating whether <see cref="Spin()"/> is invoked on calls to <see cref="ToString()"/>.
        /// </summary>
        public bool SpinOnToString { get; }

        /// <summary>
        ///     Advances the spinner by one character.
        /// </summary>
        public void Spin()
        {
            Frame = ++Frame % Frames.Count();
        }

        /// <summary>
        ///     Returns the current display text of the spinner.
        /// </summary>
        /// <returns>The current display text of the spinner.</returns>
        public override string ToString()
        {
            if (Format.HiddenWhen())
            {
                return string.Empty;
            }

            if (Format.EmptyWhen())
            {
                return new string(Format.Empty, Format.Width + 1);
            }

            if (SpinOnToString)
            {
                Spin();
            }

            var builder = new StringBuilder();
            builder.Append(new string(Format.Pad, Format.PaddingLeft));
            builder.Append(Format.Left);

            if (Format.CompleteWhen())
            {
                builder.Append(Format.Complete);
            }
            else
            {
                builder.Append(Frames.ToArray()[Frame]);
            }

            builder.Append(Format.Right);
            builder.Append(new string(Format.Pad, Format.PaddingRight));

            return builder.ToString();
        }
    }

    /// <summary>
    ///     Formatting options for spinner displays.
    /// </summary>
    public class SpinnerFormat : ProgressFormat
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SpinnerFormat"/> class.
        /// </summary>
        /// <param name="empty">The character representing empty space.</param>
        /// <param name="complete">The character to display when <paramref name="completeWhen"/> evaluates to true.</param>
        /// <param name="left">The string to prepend to the left side of the display.</param>
        /// <param name="right">The string to append to the right side of the display.</param>
        /// <param name="paddingLeft">
        ///     The amount of padding, in number of characters, to prepend to the left side of the display.
        /// </param>
        /// <param name="paddingRight">
        ///     The amount of padding, in number of characters, to append to the right side of the display.
        /// </param>
        /// <param name="pad">The character used for padding.</param>
        /// <param name="completeWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="complete"/> character..
        /// </param>
        /// <param name="emptyWhen">
        ///     The function used to determine whether the display should be composed of only the <paramref name="empty"/> character.
        /// </param>
        /// <param name="hiddenWhen">
        ///     The function used to determine whether the display should be composed of only a zero-length string.
        /// </param>
        public SpinnerFormat(
            char empty = ' ',
            char complete = '√',
            string left = null,
            string right = null,
            int paddingLeft = 0,
            int paddingRight = 0,
            char pad = ' ',
            Func<bool> completeWhen = null,
            Func<bool> emptyWhen = null,
            Func<bool> hiddenWhen = null)
            : base(empty, left, right, paddingLeft, paddingRight, pad, emptyWhen, hiddenWhen)
        {
            Complete = complete;
            CompleteWhen = completeWhen ?? (() => false);
        }

        /// <summary>
        ///     Gets the function used to determine whether the display should be composed of only the <see cref="Complete"/> character.
        /// </summary>
        public Func<bool> CompleteWhen { get; }

        /// <summary>
        ///     Gets the character to display when <see cref="CompleteWhen"/> evaluates to true.
        /// </summary>
        public char Complete { get; }
    }
}