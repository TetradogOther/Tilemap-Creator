﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TMC
{
    public class NumberBox : TextBox
    {
        public int Value
        {
            get
            {
                int result;
                if (int.TryParse(Text, out result))
                    return result;

                return 0;
            }
            set
            {
                Text = value.ToString();
            }
        }

        public int MinimumValue { get; set; }
        public int MaximumValue { get; set; } 

        public NumberBox()
        {
        	MaximumValue=int.MaxValue - 1;
        	MinimumValue=0;
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;

            base.OnKeyPress(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (Value < MinimumValue)
                Value = MinimumValue;
            if (Value > MaximumValue)
                Value = MaximumValue;
        }
    }

    public class NumberComboBox : ComboBox
    {
        public int Value
        {
            get
            {
                int result;
                if (int.TryParse(Text, out result))
                    return result;

                return 0;
            }
            set
            {
                Text = value.ToString();
            }
        }

        public int MinimumValue { get; set; }
        public int MaximumValue { get; set; }

        public NumberComboBox()
        {
        	MinimumValue=0;
        	MaximumValue=int.MaxValue-1;
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;

            base.OnKeyPress(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (Value < MinimumValue)
                Value = MinimumValue;
            if (Value > MaximumValue)
                Value = MaximumValue;
        }
    }
}
