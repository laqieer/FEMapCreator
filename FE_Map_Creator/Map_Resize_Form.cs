// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Map_Resize_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

#nullable disable
namespace FE_Map_Creator;

public class Map_Resize_Form : Form
{
  private int Width;
  private int Height;
  private IContainer components;
  private TableLayoutPanel ResizeTable;
  private FlowLayoutPanel DownResizeFlowPanel;
  private NumericUpDown DownResizeSpinner;
  private FlowLayoutPanel RightResizeFlowPanel;
  private NumericUpDown RightResizeSpinner;
  private FlowLayoutPanel UpResizeFlowPanel;
  private NumericUpDown UpResizeSpinner;
  private Label WidthResizeLabel;
  private FlowLayoutPanel LeftResizeFlowPanel;
  private NumericUpDown LeftResizeSpinner;
  private Label xResizeLabel;
  private Button ResizeOkayButton;
  private Button ResizeCancelButton;
  private FlowLayoutPanel flowLayoutPanel1;
  private FlowLayoutPanel flowLayoutPanel2;
  private Label HeightResizeLabel;

  public int up => (int) this.UpResizeSpinner.Value;

  public int left => (int) this.LeftResizeSpinner.Value;

  public int right => (int) this.RightResizeSpinner.Value;

  public int down => (int) this.DownResizeSpinner.Value;

  public Map_Resize_Form(int width, int height)
  {
    this.InitializeComponent();
    this.Width = width;
    this.Height = height;
    this.WidthResizeLabel.Text = this.Width.ToString();
    this.HeightResizeLabel.Text = this.Height.ToString();
  }

  private void UpResizeSpinner_ValueChanged(object sender, EventArgs e)
  {
    this.HeightResizeLabel.Text = (this.UpResizeSpinner.Value + this.DownResizeSpinner.Value + (Decimal) this.Height).ToString();
  }

  private void LeftResizeSpinner_ValueChanged(object sender, EventArgs e)
  {
    this.WidthResizeLabel.Text = (this.LeftResizeSpinner.Value + this.RightResizeSpinner.Value + (Decimal) this.Width).ToString();
  }

  private void RightResizeSpinner_ValueChanged(object sender, EventArgs e)
  {
    this.WidthResizeLabel.Text = (this.LeftResizeSpinner.Value + this.RightResizeSpinner.Value + (Decimal) this.Width).ToString();
  }

  private void DownResizeSpinner_ValueChanged(object sender, EventArgs e)
  {
    this.HeightResizeLabel.Text = (this.UpResizeSpinner.Value + this.DownResizeSpinner.Value + (Decimal) this.Height).ToString();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    this.ResizeTable = new TableLayoutPanel();
    this.DownResizeFlowPanel = new FlowLayoutPanel();
    this.DownResizeSpinner = new NumericUpDown();
    this.RightResizeFlowPanel = new FlowLayoutPanel();
    this.RightResizeSpinner = new NumericUpDown();
    this.UpResizeFlowPanel = new FlowLayoutPanel();
    this.UpResizeSpinner = new NumericUpDown();
    this.WidthResizeLabel = new Label();
    this.LeftResizeFlowPanel = new FlowLayoutPanel();
    this.LeftResizeSpinner = new NumericUpDown();
    this.xResizeLabel = new Label();
    this.ResizeOkayButton = new Button();
    this.ResizeCancelButton = new Button();
    this.flowLayoutPanel1 = new FlowLayoutPanel();
    this.flowLayoutPanel2 = new FlowLayoutPanel();
    this.HeightResizeLabel = new Label();
    this.ResizeTable.SuspendLayout();
    this.DownResizeFlowPanel.SuspendLayout();
    this.DownResizeSpinner.BeginInit();
    this.RightResizeFlowPanel.SuspendLayout();
    this.RightResizeSpinner.BeginInit();
    this.UpResizeFlowPanel.SuspendLayout();
    this.UpResizeSpinner.BeginInit();
    this.LeftResizeFlowPanel.SuspendLayout();
    this.LeftResizeSpinner.BeginInit();
    this.flowLayoutPanel1.SuspendLayout();
    this.flowLayoutPanel2.SuspendLayout();
    this.SuspendLayout();
    this.ResizeTable.ColumnCount = 3;
    this.ResizeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33332f));
    this.ResizeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334f));
    this.ResizeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334f));
    this.ResizeTable.Controls.Add((Control) this.DownResizeFlowPanel, 1, 2);
    this.ResizeTable.Controls.Add((Control) this.RightResizeFlowPanel, 2, 1);
    this.ResizeTable.Controls.Add((Control) this.UpResizeFlowPanel, 1, 0);
    this.ResizeTable.Controls.Add((Control) this.LeftResizeFlowPanel, 0, 1);
    this.ResizeTable.Controls.Add((Control) this.flowLayoutPanel1, 0, 0);
    this.ResizeTable.Controls.Add((Control) this.flowLayoutPanel2, 2, 2);
    this.ResizeTable.Dock = DockStyle.Fill;
    this.ResizeTable.Location = new Point(0, 0);
    this.ResizeTable.Name = "ResizeTable";
    this.ResizeTable.RowCount = 3;
    this.ResizeTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
    this.ResizeTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
    this.ResizeTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
    this.ResizeTable.Size = new Size(253, 106);
    this.ResizeTable.TabIndex = 0;
    this.DownResizeFlowPanel.Controls.Add((Control) this.DownResizeSpinner);
    this.DownResizeFlowPanel.Dock = DockStyle.Fill;
    this.DownResizeFlowPanel.Location = new Point(84, 52);
    this.DownResizeFlowPanel.Margin = new Padding(0);
    this.DownResizeFlowPanel.Name = "DownResizeFlowPanel";
    this.DownResizeFlowPanel.Size = new Size(84, 54);
    this.DownResizeFlowPanel.TabIndex = 2;
    this.DownResizeSpinner.Location = new Point(3, 3);
    this.DownResizeSpinner.Minimum = new Decimal(new int[4]
    {
      100,
      0,
      0,
      int.MinValue
    });
    this.DownResizeSpinner.Name = "DownResizeSpinner";
    this.DownResizeSpinner.Size = new Size(44, 20);
    this.DownResizeSpinner.TabIndex = 2;
    this.DownResizeSpinner.ValueChanged += new EventHandler(this.DownResizeSpinner_ValueChanged);
    this.RightResizeFlowPanel.Controls.Add((Control) this.RightResizeSpinner);
    this.RightResizeFlowPanel.Dock = DockStyle.Fill;
    this.RightResizeFlowPanel.Location = new Point(168, 26);
    this.RightResizeFlowPanel.Margin = new Padding(0);
    this.RightResizeFlowPanel.Name = "RightResizeFlowPanel";
    this.RightResizeFlowPanel.Size = new Size(85, 26);
    this.RightResizeFlowPanel.TabIndex = 2;
    this.RightResizeSpinner.Location = new Point(3, 3);
    this.RightResizeSpinner.Minimum = new Decimal(new int[4]
    {
      100,
      0,
      0,
      int.MinValue
    });
    this.RightResizeSpinner.Name = "RightResizeSpinner";
    this.RightResizeSpinner.Size = new Size(44, 20);
    this.RightResizeSpinner.TabIndex = 2;
    this.RightResizeSpinner.ValueChanged += new EventHandler(this.RightResizeSpinner_ValueChanged);
    this.UpResizeFlowPanel.Controls.Add((Control) this.UpResizeSpinner);
    this.UpResizeFlowPanel.Dock = DockStyle.Fill;
    this.UpResizeFlowPanel.Location = new Point(84, 0);
    this.UpResizeFlowPanel.Margin = new Padding(0);
    this.UpResizeFlowPanel.Name = "UpResizeFlowPanel";
    this.UpResizeFlowPanel.Size = new Size(84, 26);
    this.UpResizeFlowPanel.TabIndex = 0;
    this.UpResizeSpinner.Location = new Point(3, 3);
    this.UpResizeSpinner.Minimum = new Decimal(new int[4]
    {
      100,
      0,
      0,
      int.MinValue
    });
    this.UpResizeSpinner.Name = "UpResizeSpinner";
    this.UpResizeSpinner.Size = new Size(44, 20);
    this.UpResizeSpinner.TabIndex = 0;
    this.UpResizeSpinner.ValueChanged += new EventHandler(this.UpResizeSpinner_ValueChanged);
    this.WidthResizeLabel.AutoSize = true;
    this.WidthResizeLabel.Location = new Point(3, 5);
    this.WidthResizeLabel.Margin = new Padding(3, 5, 3, 0);
    this.WidthResizeLabel.Name = "WidthResizeLabel";
    this.WidthResizeLabel.Size = new Size(28, 13);
    this.WidthResizeLabel.TabIndex = 1;
    this.WidthResizeLabel.Text = "-100";
    this.LeftResizeFlowPanel.Controls.Add((Control) this.LeftResizeSpinner);
    this.LeftResizeFlowPanel.Dock = DockStyle.Fill;
    this.LeftResizeFlowPanel.Location = new Point(0, 26);
    this.LeftResizeFlowPanel.Margin = new Padding(0);
    this.LeftResizeFlowPanel.Name = "LeftResizeFlowPanel";
    this.LeftResizeFlowPanel.Size = new Size(84, 26);
    this.LeftResizeFlowPanel.TabIndex = 1;
    this.LeftResizeSpinner.Location = new Point(3, 3);
    this.LeftResizeSpinner.Minimum = new Decimal(new int[4]
    {
      100,
      0,
      0,
      int.MinValue
    });
    this.LeftResizeSpinner.Name = "LeftResizeSpinner";
    this.LeftResizeSpinner.Size = new Size(44, 20);
    this.LeftResizeSpinner.TabIndex = 2;
    this.LeftResizeSpinner.ValueChanged += new EventHandler(this.LeftResizeSpinner_ValueChanged);
    this.xResizeLabel.AutoSize = true;
    this.xResizeLabel.Location = new Point(34, 5);
    this.xResizeLabel.Margin = new Padding(0, 5, 0, 0);
    this.xResizeLabel.Name = "xResizeLabel";
    this.xResizeLabel.Size = new Size(12, 13);
    this.xResizeLabel.TabIndex = 3;
    this.xResizeLabel.Text = "x";
    this.ResizeOkayButton.DialogResult = DialogResult.OK;
    this.ResizeOkayButton.Location = new Point(3, 3);
    this.ResizeOkayButton.Name = "ResizeOkayButton";
    this.ResizeOkayButton.Size = new Size(44, 21);
    this.ResizeOkayButton.TabIndex = 4;
    this.ResizeOkayButton.Text = "OK";
    this.ResizeOkayButton.UseVisualStyleBackColor = true;
    this.ResizeCancelButton.DialogResult = DialogResult.Cancel;
    this.ResizeCancelButton.Location = new Point(3, 30);
    this.ResizeCancelButton.Name = "ResizeCancelButton";
    this.ResizeCancelButton.Size = new Size(54, 21);
    this.ResizeCancelButton.TabIndex = 5;
    this.ResizeCancelButton.Text = "Cancel";
    this.ResizeCancelButton.UseVisualStyleBackColor = true;
    this.flowLayoutPanel1.Controls.Add((Control) this.WidthResizeLabel);
    this.flowLayoutPanel1.Controls.Add((Control) this.xResizeLabel);
    this.flowLayoutPanel1.Controls.Add((Control) this.HeightResizeLabel);
    this.flowLayoutPanel1.Dock = DockStyle.Fill;
    this.flowLayoutPanel1.Location = new Point(0, 0);
    this.flowLayoutPanel1.Margin = new Padding(0);
    this.flowLayoutPanel1.Name = "flowLayoutPanel1";
    this.flowLayoutPanel1.Size = new Size(84, 26);
    this.flowLayoutPanel1.TabIndex = 0;
    this.flowLayoutPanel2.Controls.Add((Control) this.ResizeOkayButton);
    this.flowLayoutPanel2.Controls.Add((Control) this.ResizeCancelButton);
    this.flowLayoutPanel2.Dock = DockStyle.Fill;
    this.flowLayoutPanel2.Location = new Point(168, 52);
    this.flowLayoutPanel2.Margin = new Padding(0);
    this.flowLayoutPanel2.Name = "flowLayoutPanel2";
    this.flowLayoutPanel2.Size = new Size(85, 54);
    this.flowLayoutPanel2.TabIndex = 0;
    this.HeightResizeLabel.AutoSize = true;
    this.HeightResizeLabel.Location = new Point(49, 5);
    this.HeightResizeLabel.Margin = new Padding(3, 5, 3, 0);
    this.HeightResizeLabel.Name = "HeightResizeLabel";
    this.HeightResizeLabel.Size = new Size(28, 13);
    this.HeightResizeLabel.TabIndex = 4;
    this.HeightResizeLabel.Text = "-100";
    this.AcceptButton = (IButtonControl) this.ResizeOkayButton;
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.CancelButton = (IButtonControl) this.ResizeCancelButton;
    this.ClientSize = new Size(253, 106);
    this.Controls.Add((Control) this.ResizeTable);
    this.MaximumSize = new Size(269, 144 /*0x90*/);
    this.MinimumSize = new Size(269, 144 /*0x90*/);
    this.Name = nameof (Map_Resize_Form);
    this.Text = nameof (Map_Resize_Form);
    this.ResizeTable.ResumeLayout(false);
    this.DownResizeFlowPanel.ResumeLayout(false);
    this.DownResizeSpinner.EndInit();
    this.RightResizeFlowPanel.ResumeLayout(false);
    this.RightResizeSpinner.EndInit();
    this.UpResizeFlowPanel.ResumeLayout(false);
    this.UpResizeSpinner.EndInit();
    this.LeftResizeFlowPanel.ResumeLayout(false);
    this.LeftResizeSpinner.EndInit();
    this.flowLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.PerformLayout();
    this.flowLayoutPanel2.ResumeLayout(false);
    this.ResumeLayout(false);
  }
}
