using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace CabinetListGenerator
{
    public partial class Form1 : Form
    {
        private class Area
        {
            public Area(int startX, int startY, int width, int height, Color backColor)
            {
                Children = new List<Area>();

                StartX = startX;
                StartY = startY;
                Width = width;
                Height = height;
                BackColor = backColor;
            }

            public int StartX { get; set; }
            public int StartY { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public Color BackColor { get; set; }

            public List<Area> Children { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            Results.Text = "";

            decimal horizontalUsableSpace = 
                ImageWidth.Value - (
                    OutsideBorderThickness.Value +
                    HeaderTextBoxHeight.Value + 
                    MajorBorderThickness.Value +
                    ((ColumnCount.Value - 1M) * MajorBorderThickness.Value) +
                    OutsideBorderThickness.Value);

            decimal verticalUsableSpace = 
                ImageHeight.Value - (
                    OutsideBorderThickness.Value +
                    HeaderTextBoxHeight.Value + 
                    MinorBorderThickness.Value + 
                    SubHeaderTextBoxHeight.Value + 
                    MajorBorderThickness.Value + 
                    ((RowCount.Value - 1M) * MajorBorderThickness.Value) +
                    OutsideBorderThickness.Value +
                    TextBoxHeight.Value);

            int textBorderThickness = Convert.ToInt32(TextBorderThickness.Value);
            int textBoxHeight = Convert.ToInt32(TextBoxHeight.Value);
            int textLineCount = Convert.ToInt32(verticalUsableSpace / (textBorderThickness + textBoxHeight));
            int textLineCountPerRow = Convert.ToInt32(textLineCount / RowCount.Value);

            //decimal remainingTextSpace = verticalUsableSpace - (textLineCountPerRow * RowCount.Value);

            int columnWidths = Convert.ToInt32(horizontalUsableSpace / ColumnCount.Value);

            int minorBorderThickness = Convert.ToInt32(MinorBorderThickness.Value);

            var subColumnWidths = new List<int>();

            if (SubColumnCount.Value > 1M)
            {
                CalculateSubColumnWidths(subColumnWidths, columnWidths, Convert.ToInt32(SubColumnCount.Value), minorBorderThickness);
            }

            int imageWidth = Convert.ToInt32(ImageWidth.Value);
            int imageHeight = Convert.ToInt32(ImageHeight.Value);

            Area image = new Area(0, 0, imageWidth, imageHeight, Color.Transparent);

            //Outside Borders
            int outsideBorderThickness = Convert.ToInt32(OutsideBorderThickness.Value);
            int verticalAreaHeight = imageHeight - (outsideBorderThickness * 2);
            image.Children.Add(new Area(startX: 0, 
                                        startY: 0, 
                                        width: imageWidth, 
                                        height: outsideBorderThickness, 
                                        backColor: OutsideBorderColor.BackColor));
            image.Children.Add(new Area(startX: 0, 
                                        startY: outsideBorderThickness, 
                                        width: outsideBorderThickness, 
                                        height: verticalAreaHeight, 
                                        backColor: OutsideBorderColor.BackColor));
            image.Children.Add(new Area(startX: (imageWidth - 1) - outsideBorderThickness, 
                                        startY: outsideBorderThickness, 
                                        width: outsideBorderThickness, 
                                        height: verticalAreaHeight, 
                                        backColor: OutsideBorderColor.BackColor));
            image.Children.Add(new Area(startX: 0, 
                                        startY: (imageHeight - 1) - outsideBorderThickness, 
                                        width: imageWidth, 
                                        height: outsideBorderThickness, 
                                        backColor: OutsideBorderColor.BackColor));

            //Vertical Areas and Vertical Major Borders
            int headerTextBoxHeight = Convert.ToInt32(HeaderTextBoxHeight.Value);
            int runningColumnX = 0;
            var rowHeaders = new Area(startX: runningColumnX += outsideBorderThickness, 
                                      startY: outsideBorderThickness, 
                                      width: headerTextBoxHeight, 
                                      height: verticalAreaHeight, 
                                      backColor: Color.Transparent);

            int majorBorderThickness = Convert.ToInt32(MajorBorderThickness.Value);
            int subHeaderTextBoxHeight = Convert.ToInt32(SubHeaderTextBoxHeight.Value);
            int rowCount = Convert.ToInt32(RowCount.Value);
            int rowHeights = (textLineCountPerRow * (textBoxHeight + textBorderThickness)) - textBorderThickness;

            PopulateRowHeaderVerticalArea(parent: rowHeaders, 
                                          nullHeight: headerTextBoxHeight + minorBorderThickness + subHeaderTextBoxHeight + majorBorderThickness, 
                                          rowCount: rowCount, 
                                          rowHeight: rowHeights, 
                                          majorBorderThickness: majorBorderThickness);

            image.Children.Add(rowHeaders);

            image.Children.Add(new Area(startX: runningColumnX += headerTextBoxHeight, 
                                        startY: outsideBorderThickness, 
                                        width: majorBorderThickness, 
                                        height: verticalAreaHeight, 
                                        backColor: MajorBorderColor.BackColor));

            runningColumnX += majorBorderThickness;

            for (int columnIndex = 0; columnIndex < ColumnCount.Value; columnIndex++)
            {
                var column = new Area(startX: runningColumnX + ((columnWidths + majorBorderThickness) * columnIndex),
                                      startY: outsideBorderThickness,
                                      width: columnWidths,
                                      height: verticalAreaHeight,
                                      backColor: Color.Transparent);

                PopulateColumn(parent: column, 
                               headerTextBoxHeight: headerTextBoxHeight, 
                               minorBorderThickness: minorBorderThickness, 
                               subHeaderTextBoxHeight: subHeaderTextBoxHeight, 
                               majorBorderThickness: majorBorderThickness, 
                               rowCount: rowCount,
                               textBoxHeight: textBoxHeight, 
                               textBorderThickness: textBorderThickness, 
                               textLineCount: textLineCountPerRow,
                               subColumnWidths: subColumnWidths);

                image.Children.Add(column);

                if (columnIndex + 1 < ColumnCount.Value)
                {
                    image.Children.Add(new Area(startX: runningColumnX + ((columnWidths + majorBorderThickness) * columnIndex) + columnWidths,
                                                startY: outsideBorderThickness,
                                                width: majorBorderThickness,
                                                height: verticalAreaHeight,
                                                backColor: MajorBorderColor.BackColor));
                }
            }

            Bitmap outputImage = new Bitmap(imageWidth, imageHeight);

            EnumerateDescendents(image, outputImage);

            outputImage.Save(string.IsNullOrEmpty(Filename.Text) ? "list.png" : Filename.Text);

            /*
            for (int x = 0; x < 2250; x++)
            {
                for (int y = 0; y < 3000; y++)
                {

                }
            }
             */
        }

        private void EnumerateDescendents(Area parent, Bitmap outputImage)
        {
            foreach (var child in parent.Children)
            {
                Results.Text +=
                    "Area: {\r\n" +
                    string.Format("    startX: {0}\r\n", child.StartX.ToString()) +
                    string.Format("    startY: {0}\r\n", child.StartY.ToString()) +
                    string.Format("     width: {0}\r\n", child.Width.ToString()) +
                    string.Format("    height: {0}\r\n", child.Height.ToString()) +
                    string.Format(" backColor: {0}\r\n", child.BackColor.ToString()) +
                    "}\r\n\r\n";

                if (child.BackColor != Color.Transparent)
                {
                    for (int x = child.StartX; x < child.StartX + child.Width; x++)
                    {
                        for (int y = child.StartY; y < child.StartY + child.Height; y++)
                        {
                            outputImage.SetPixel(x, y, child.BackColor);
                        }
                    }
                }

                if (child.Children.Count > 0)
                {
                    EnumerateDescendents(child, outputImage);
                }
            }
        }

        private void CalculateSubColumnWidths(List<int> widths, decimal remainingSpace, int remainingDivisions, int minorBorderThickness)
        {
            int columnWidth = Convert.ToInt32(remainingSpace / SubColumnRatio.Value);
            decimal leftoverSpace = remainingSpace - (columnWidth + minorBorderThickness);

            widths.Add(columnWidth);

            if (remainingDivisions - 1 > 1)
            {
                CalculateSubColumnWidths(widths, leftoverSpace, remainingDivisions - 1, minorBorderThickness);
            }
            else
            {
                widths.Add(Convert.ToInt32(leftoverSpace));
            }
        }

        private void PopulateRowHeaderVerticalArea(Area parent, int nullHeight, int rowCount, int rowHeight, int majorBorderThickness)
        {
            parent.Children.Add(new Area(startX: parent.StartX,
                                         startY: parent.StartY,
                                         width: parent.Width,
                                         height: nullHeight,
                                         backColor: OutsideBorderColor.BackColor));

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                parent.Children.Add(new Area(startX: parent.StartX,
                                             startY: parent.StartY + nullHeight + ((rowHeight + majorBorderThickness) * rowIndex),
                                             width: parent.Width,
                                             height: rowHeight,
                                             backColor: Color.Transparent));

                if (rowIndex + 1 < rowCount)
                {
                    parent.Children.Add(new Area(startX: parent.StartX,
                                                 startY: parent.StartY + nullHeight + ((rowHeight + majorBorderThickness) * rowIndex) + rowHeight,
                                                 width: parent.Width,
                                                 height: majorBorderThickness,
                                                 backColor: MajorBorderColor.BackColor));
                }

            }
        }

        private void PopulateColumn(Area parent, int headerTextBoxHeight, int minorBorderThickness, int subHeaderTextBoxHeight, int majorBorderThickness, int rowCount, int textBoxHeight, int textBorderThickness, int textLineCount, List<int> subColumnWidths)
        {
            int runningStartY = 0;

            parent.Children.Add(new Area(startX: parent.StartX,
                                         startY: runningStartY += parent.StartY,
                                         width: parent.Width,
                                         height: headerTextBoxHeight,
                                         backColor: Color.Transparent));

            parent.Children.Add(new Area(startX: parent.StartX,
                                         startY: runningStartY += headerTextBoxHeight,
                                         width: parent.Width,
                                         height: minorBorderThickness,
                                         backColor: MinorBorderColor.BackColor));

            var subHeader = new Area(startX: parent.StartX,
                                     startY: runningStartY += minorBorderThickness,
                                     width: parent.Width,
                                     height: subHeaderTextBoxHeight,
                                     backColor: Color.Transparent);

            int runningSubHeaderSubColumnX = 0;

            foreach (var subColumnWidth in subColumnWidths)
            {
                subHeader.Children.Add(new Area(startX: subHeader.StartX + runningSubHeaderSubColumnX,
                                                startY: subHeader.StartY,
                                                width: subColumnWidth,
                                                height: textBoxHeight,
                                                backColor: Color.Transparent));

                subHeader.Children.Add(new Area(startX: subHeader.StartX + (runningSubHeaderSubColumnX += subColumnWidth),
                                                startY: subHeader.StartY,
                                                width: minorBorderThickness,
                                                height: textBoxHeight,
                                                backColor: MinorBorderColor.BackColor));

                runningSubHeaderSubColumnX += minorBorderThickness;
            }

            parent.Children.Add(subHeader);

            parent.Children.Add(new Area(startX: parent.StartX,
                                         startY: runningStartY += subHeaderTextBoxHeight,
                                         width: parent.Width,
                                         height: majorBorderThickness,
                                         backColor: MajorBorderColor.BackColor));

            runningStartY += majorBorderThickness;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                for (int textLineIndex = 0; textLineIndex < textLineCount; textLineIndex++)
                {
                    var textBox = new Area(startX: parent.StartX,
                                           startY: runningStartY +
                                               (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                               (textLineIndex * (textBoxHeight + textBorderThickness)),
                                           width: parent.Width,
                                           height: textBoxHeight,
                                           backColor: Color.Transparent);

                    int runningSubColumnX = 0;

                    foreach (var subColumnWidth in subColumnWidths)
                    {
                        textBox.Children.Add(new Area(startX: textBox.StartX + runningSubColumnX,
                                                     startY: textBox.StartY,
                                                     width: subColumnWidth,
                                                     height: textBoxHeight,
                                                     backColor: Color.Transparent));

                        textBox.Children.Add(new Area(startX: textBox.StartX + (runningSubColumnX += subColumnWidth),
                                                     startY: textBox.StartY,
                                                     width: minorBorderThickness,
                                                     height: textBoxHeight,
                                                     backColor: MinorBorderColor.BackColor));

                        runningSubColumnX += minorBorderThickness;
                    }

                    parent.Children.Add(textBox);

                    //Only used for the last textbox of the last row
                    int remainingSpace = parent.Height - ((((textLineCount * (textBoxHeight + textBorderThickness)) - textBorderThickness) * rowCount) + (majorBorderThickness * (rowCount - 1)) + (headerTextBoxHeight + minorBorderThickness + subHeaderTextBoxHeight + majorBorderThickness));

                    if (textLineIndex + 1 < textLineCount)
                    {
                        var border = new Area(startX: parent.StartX,
                                              startY: runningStartY +
                                                  (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                                  (textLineIndex * (textBoxHeight + textBorderThickness)) +
                                                  textBoxHeight,
                                              width: parent.Width,
                                              height: textBorderThickness,
                                              backColor: Color.Transparent);

                        int runningSubColumnBorderX = 0;

                        foreach (var subColumnWidth in subColumnWidths)
                        {
                            border.Children.Add(new Area(startX: border.StartX + runningSubColumnBorderX,
                                                         startY: border.StartY,
                                                         width: subColumnWidth,
                                                         height: textBorderThickness,
                                                         backColor: TextBorderColor.BackColor));

                            border.Children.Add(new Area(startX: border.StartX + (runningSubColumnBorderX += subColumnWidth),
                                                         startY: border.StartY,
                                                         width: minorBorderThickness,
                                                         height: textBorderThickness,
                                                         backColor: MinorBorderColor.BackColor));

                            runningSubColumnBorderX += minorBorderThickness;
                        }

                        parent.Children.Add(border);
                    }
                    else if (
                        (rowIndex + 1 >= rowCount) &&
                        (remainingSpace > (textBoxHeight + textBorderThickness)))
                    {
                        parent.Children.Add(new Area(startX: parent.StartX,
                                                     startY: runningStartY +
                                                         (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                                         (textLineIndex * (textBoxHeight + textBorderThickness)) +
                                                         textBoxHeight,
                                                     width: parent.Width,
                                                     height: textBorderThickness,
                                                     backColor: TextBorderColor.BackColor));

                        var lastBox = new Area(startX: parent.StartX,
                                               startY: runningStartY +
                                                   (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                                   (textLineIndex * (textBoxHeight + textBorderThickness)) +
                                                   textBoxHeight + textBorderThickness,
                                               width: parent.Width,
                                               height: remainingSpace - textBorderThickness,
                                               backColor: Color.Transparent);

                        int runningLastSubColumnX = 0;

                        foreach (var subColumnWidth in subColumnWidths)
                        {
                            lastBox.Children.Add(new Area(startX: lastBox.StartX + runningLastSubColumnX,
                                                         startY: lastBox.StartY,
                                                         width: subColumnWidth,
                                                         height: remainingSpace - textBorderThickness,
                                                         backColor: Color.Transparent));

                            lastBox.Children.Add(new Area(startX: lastBox.StartX + (runningLastSubColumnX += subColumnWidth),
                                                         startY: lastBox.StartY,
                                                         width: minorBorderThickness,
                                                         height: remainingSpace - textBorderThickness,
                                                         backColor: MinorBorderColor.BackColor));

                            runningLastSubColumnX += minorBorderThickness;
                        }

                        parent.Children.Add(lastBox);
                    }
                    else if (rowIndex + 1 >= rowCount)
                    {
                        var lastBoxSpace = new Area(startX: parent.StartX,
                                               startY: runningStartY +
                                                   (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                                   (textLineIndex * (textBoxHeight + textBorderThickness)) + 
                                                   textBoxHeight,
                                               width: parent.Width,
                                               height: remainingSpace,
                                               backColor: Color.Transparent);

                        int runningLastBoxSubColumnX = 0;

                        foreach (var subColumnWidth in subColumnWidths)
                        {
                            lastBoxSpace.Children.Add(new Area(startX: lastBoxSpace.StartX + runningLastBoxSubColumnX,
                                                         startY: lastBoxSpace.StartY,
                                                         width: subColumnWidth,
                                                         height: remainingSpace,
                                                         backColor: Color.Transparent));

                            lastBoxSpace.Children.Add(new Area(startX: lastBoxSpace.StartX + (runningLastBoxSubColumnX += subColumnWidth),
                                                         startY: lastBoxSpace.StartY,
                                                         width: minorBorderThickness,
                                                         height: remainingSpace,
                                                         backColor: MinorBorderColor.BackColor));

                            runningLastBoxSubColumnX += minorBorderThickness;
                        }

                        parent.Children.Add(lastBoxSpace);
                    }                        
                }

                if (rowIndex + 1 < rowCount)
                {
                    parent.Children.Add(new Area(startX: parent.StartX,
                                                 startY: runningStartY +
                                                     (rowIndex * ((((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness) + majorBorderThickness)) +
                                                     (((textBoxHeight + textBorderThickness) * textLineCount) - textBorderThickness),
                                                 width: parent.Width,
                                                 height: majorBorderThickness,
                                                 backColor: MajorBorderColor.BackColor));
                }
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            var senderButton = sender as Button;

            UserColorSelectorDialog.Color = senderButton.BackColor;

            if (UserColorSelectorDialog.ShowDialog() == DialogResult.OK)
            {
                senderButton.BackColor = UserColorSelectorDialog.Color;
            }
        }
    }
}
