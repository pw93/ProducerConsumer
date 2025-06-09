using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WinFormsApp5
{
    public partial class Form2 : Form
    {
       

        public Form2()
        {
            InitializeComponent();
           
        }


        public struct RectF
        {
            public float X, Y, Width, Height;

            public RectF(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public override string ToString()
            {
                return $"Rect(X: {X}, Y: {Y}, W: {Width}, H: {Height})";
            }
        }
   
        public class Layout
        {
            public RectF Rect;

            public Layout(RectF rect)
            {
                Rect = rect;
            }

            public void ApplyPadding(int top, int right, int bottom, int left)
            {
                Rect = new RectF(
                    Rect.X + left,
                    Rect.Y + top,
                    Rect.Width - left - right,
                    Rect.Height - top - bottom
                );
            }

            public List<Layout> SetVerticalLayout(
                        List<float> ratios,
                        int paddingTop = 0,
                        int paddingRight = 0,
                        int paddingBottom = 0,
                        int paddingLeft = 0,
                        int spacing = 0)
            {
                var result = new List<Layout>();
                float totalRatio = ratios.Sum();
                int count = ratios.Count;

                float totalSpacing = spacing * (count - 1);
                float availableHeight = Rect.Height - paddingTop - paddingBottom - totalSpacing;
                float yOffset = Rect.Y + paddingTop;

                foreach (var ratio in ratios)
                {
                    float normalized = ratio / totalRatio;
                    float height = availableHeight * normalized;

                    result.Add(new Layout(new RectF(
                        Rect.X + paddingLeft,
                        yOffset,
                        Rect.Width - paddingLeft - paddingRight,
                        height
                    )));

                    yOffset += height + spacing;
                }

                return result;
            }

            public List<Layout> SetHorizontalLayout(
                    List<float> ratios,
                    int paddingTop = 0,
                    int paddingRight = 0,
                    int paddingBottom = 0,
                    int paddingLeft = 0,
                    int spacing = 0)
            {
                var result = new List<Layout>();
                float totalRatio = ratios.Sum();
                int count = ratios.Count;

                float totalSpacing = spacing * (count - 1);
                float availableWidth = Rect.Width - paddingLeft - paddingRight - totalSpacing;
                float xOffset = Rect.X + paddingLeft;

                foreach (var ratio in ratios)
                {
                    float normalized = ratio / totalRatio;
                    float width = availableWidth * normalized;

                    result.Add(new Layout(new RectF(
                        xOffset,
                        Rect.Y + paddingTop,
                        width,
                        Rect.Height - paddingTop - paddingBottom
                    )));

                    xOffset += width + spacing;
                }

                return result;
            }

            public override string ToString() => Rect.ToString();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            CalculateLayout();
        }

        private void Form2_Resize(object sender, EventArgs e)
        {
            //LayoutControls();
        }     
        void CalculateLayout()
        {
            RectF rootRect = new RectF(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            Layout layout = new Layout(rootRect);

            // 2. Vertical split: 30% - 30% - 10%
            var layout1 = layout.SetVerticalLayout(new List<float> { 5, 3, 2 }, 20, 20, 20,20, 20);

            // 3. Horizontal split in first section
            var layout2 = layout1[0].SetHorizontalLayout(new List<float> { 6, 2 },0,0,0,0,20);

            //layout2[0].SetPadding(30, 0, 30, 0);

            // 4. Move/resize elements
            SetControlBounds(button1, layout2[0].Rect);
            SetControlBounds(button2, layout2[1].Rect);
            SetControlBounds(richTextBox1, layout1[1].Rect);
            SetControlBounds(label1, layout1[2].Rect);
        }

        private void Form2_SizeChanged(object sender, EventArgs e)
        {
            CalculateLayout();
        }
        private void SetControlBounds(Control ctrl, RectF r)
        {
            //ctrl.SetBounds((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            //ctrl.Location = new Point((int)r.X, (int)r.Y);
            //ctrl.Size = new Size((int)r.Width, (int)r.Height);

            var newBounds = new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            if (ctrl.Bounds != newBounds)
            {
                ctrl.Bounds = newBounds;
            }
        }
       

        private void Form2_Resize_1(object sender, EventArgs e)
        {
           
        }
    }
}
