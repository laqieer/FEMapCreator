// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Mar_Import_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

#nullable disable
namespace FE_Map_Creator;

public class Mar_Import_Form : Form
{
  protected bool updating;
  protected int Map_Size;
  private IContainer components;
  private Button OkayButton;
  private NumericUpDown WidthSpinner;
  private NumericUpDown HeightSpinner;
  private Label WidthLabel;
  private Label HeightLabel;
  private Label FilesizeLabel;
  private Label MapSizeLabel;

  public int map_width => (int) this.WidthSpinner.Value;

  public int map_height => (int) this.HeightSpinner.Value;

  public Mar_Import_Form(int size, int width)
  {
    this.InitializeComponent();
    this.Map_Size = size;
    this.updating = true;
    this.WidthSpinner.Value = (Decimal) width;
    this.HeightSpinner.Value = (Decimal) (int) ((Decimal) this.Map_Size / this.WidthSpinner.Value);
    this.updating = false;
    this.update_labels();
  }

  protected void update_labels()
  {
    this.FilesizeLabel.Text = $"{this.Map_Size} tiles";
    this.MapSizeLabel.Text = $"{this.WidthSpinner.Value} x {this.HeightSpinner.Value} = {this.WidthSpinner.Value * this.HeightSpinner.Value}";
    this.OkayButton.Enabled = this.WidthSpinner.Value * this.HeightSpinner.Value <= (Decimal) this.Map_Size;
  }

  private void WidthSpinner_ValueChanged(object sender, EventArgs e)
  {
    if (this.updating)
      return;
    this.update_labels();
  }

  private void HeightSpinner_ValueChanged(object sender, EventArgs e)
  {
    if (this.updating)
      return;
    this.update_labels();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    this.OkayButton = new Button();
    this.WidthSpinner = new NumericUpDown();
    this.HeightSpinner = new NumericUpDown();
    this.WidthLabel = new Label();
    this.HeightLabel = new Label();
    this.FilesizeLabel = new Label();
    this.MapSizeLabel = new Label();
    this.WidthSpinner.BeginInit();
    this.HeightSpinner.BeginInit();
    this.SuspendLayout();
    this.OkayButton.DialogResult = DialogResult.OK;
    this.OkayButton.Location = new Point(180, 4);
    this.OkayButton.Name = "OkayButton";
    this.OkayButton.Size = new Size(51, 23);
    this.OkayButton.TabIndex = 0;
    this.OkayButton.Text = "Okay";
    this.OkayButton.UseVisualStyleBackColor = true;
    this.WidthSpinner.Location = new Point(50, 7);
    this.WidthSpinner.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.WidthSpinner.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.WidthSpinner.Name = "WidthSpinner";
    this.WidthSpinner.Size = new Size(51, 20);
    this.WidthSpinner.TabIndex = 1;
    this.WidthSpinner.Value = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.WidthSpinner.ValueChanged += new EventHandler(this.WidthSpinner_ValueChanged);
    this.HeightSpinner.Location = new Point(50, 33);
    this.HeightSpinner.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.HeightSpinner.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.HeightSpinner.Name = "HeightSpinner";
    this.HeightSpinner.Size = new Size(51, 20);
    this.HeightSpinner.TabIndex = 2;
    this.HeightSpinner.Value = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.HeightSpinner.ValueChanged += new EventHandler(this.HeightSpinner_ValueChanged);
    this.WidthLabel.AutoSize = true;
    this.WidthLabel.Location = new Point(6, 9);
    this.WidthLabel.Name = "WidthLabel";
    this.WidthLabel.Size = new Size(38, 13);
    this.WidthLabel.TabIndex = 3;
    this.WidthLabel.Text = "Width:";
    this.HeightLabel.AutoSize = true;
    this.HeightLabel.Location = new Point(6, 35);
    this.HeightLabel.Name = "HeightLabel";
    this.HeightLabel.Size = new Size(41, 13);
    this.HeightLabel.TabIndex = 4;
    this.HeightLabel.Text = "Height:";
    this.FilesizeLabel.AutoSize = true;
    this.FilesizeLabel.Location = new Point(107, 9);
    this.FilesizeLabel.Name = "FilesizeLabel";
    this.FilesizeLabel.Size = new Size(34, 13);
    this.FilesizeLabel.TabIndex = 5;
    this.FilesizeLabel.Text = "0 tiles";
    this.MapSizeLabel.AutoSize = true;
    this.MapSizeLabel.Location = new Point(107, 35);
    this.MapSizeLabel.Name = "MapSizeLabel";
    this.MapSizeLabel.Size = new Size(48 /*0x30*/, 13);
    this.MapSizeLabel.TabIndex = 6;
    this.MapSizeLabel.Text = "0 x 0 = 0";
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.ClientSize = new Size(250, 60);
    this.Controls.Add((Control) this.MapSizeLabel);
    this.Controls.Add((Control) this.FilesizeLabel);
    this.Controls.Add((Control) this.HeightLabel);
    this.Controls.Add((Control) this.WidthLabel);
    this.Controls.Add((Control) this.HeightSpinner);
    this.Controls.Add((Control) this.WidthSpinner);
    this.Controls.Add((Control) this.OkayButton);
    this.MaximizeBox = false;
    this.MaximumSize = new Size(266, 98);
    this.MinimizeBox = false;
    this.MinimumSize = new Size(266, 98);
    this.Name = nameof (Mar_Import_Form);
    this.StartPosition = FormStartPosition.CenterParent;
    this.Text = ".mar Import Form";
    this.WidthSpinner.EndInit();
    this.HeightSpinner.EndInit();
    this.ResumeLayout(false);
    this.PerformLayout();
  }
}
