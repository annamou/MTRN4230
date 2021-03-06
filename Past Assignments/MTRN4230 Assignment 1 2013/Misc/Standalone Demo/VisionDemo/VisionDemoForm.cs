/*
 * Title:       Vision Demo
 * Author:      Matthew Grosvenor (mgrosvenor@gmail.com)
 * Revision:    1.1
 * Status:      Fully Wokring
 * Description: A demo program to get students started on the
 *              MTRN4230 lab project. Demo contains the basics 
 *              to get started including camera control and IO
 *              control. The user interface should not be 
 *              considered representative of a good solution. 
 * History:     1.0 - Initial release
 *              1.1 - Fixed commenting and minor issues
 *    
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using UNSW_MTRN;

/* 
 * This software demonstrates SOME of the features available to 
 * the vision system.  With this program and the tutorial pack, 
 * you have all the tools you need to write a full solution to 
 * this problem. 
 * 
 */


namespace VisionDemo
{
    public partial class VisionDemoForm : Form
    {
        //A camera object
        private IEEE1394CameraDotNet theCamera;

        //An IO Card object
        private PCI1711DotNet IOCard;

        public VisionDemoForm()
        {
            //Auto generated by Visual Studio
            InitializeComponent();

            //Create the Camera object
            theCamera = new IEEE1394CameraDotNet();

            //Create the IO Card Object
            IOCard = new PCI1711DotNet();
        }

        //Take a snapshot from the camera when button is clicked 
        private void takePictureButton_Click(object sender, EventArgs e)
        {

            //If the live preview is on, turn it off while we take the picture            
            bool checkBoxChecked = displayLivePreviewCheckBox.Checked;
            if (checkBoxChecked)
            {
                //Tell the thread to stop
                displayLivePreviewCheckBox.Checked = false;

                //Wait for it to stop
                while (backgroundWorking)
                { }

            }

            //Take a picture and set it to the Picture Box image. 
            //The parameter is the shutter speed value between 0 and 4096
            imagePreviewPictureBox.Image = theCamera.AquireBitmapImage(3000);

            //Check for pins
            CheckAllPinsButton.PerformClick();

            //Refresh the picture box so that the image is displayed
            imagePreviewPictureBox.Refresh();

            //Refersh the histogram so that it is displayed (see pictureBox2_paint)
            histogramPictureBox.Refresh();

            //Restore the checkbox to whatever it was before
            displayLivePreviewCheckBox.Checked = checkBoxChecked;

        }

        private bool PinCheck(int PinNum)
        {
            // Checks a slot for a pin

            // Defining pallet properties
            const int palletborder = 10, PinCheckArea = 12, PinSpacing = 77;
            int numHighPixelsCount = 0;
             
            // Determining pin co-ordinate
            int PinY = PinNum / 5 + 1;
            int PinX = PinNum % 5;
            if (PinX == 0) { PinX = 5; PinY -= 1; }

            // Checks a certain area in a pin slot
            int PinCenX = Convert.ToInt32(PalletXPosTextBox.Text) + palletborder +
                          PinX * PinSpacing - PinCheckArea / 2;
            int PinCenY = Convert.ToInt32(PalletYPosTextBox.Text) + palletborder +
                          PinY * PinSpacing - PinCheckArea / 2;

            for (int i = 0; i < PinCheckArea; i++)
                for (int j = 0; j < PinCheckArea; j++)
                {
                    byte pixelValue = theCamera.GetPixel(PinCenX + i, PinCenY + j);
                    if (pixelValue > 200) numHighPixelsCount++;
                }

            // If the majority of pixels in the slot are highly valued,
            // there is a pin present
            if (numHighPixelsCount / (PinCheckArea ^ 2) > 0.5)
                return true;
            else
                return false;            
        }

        private void histogramPictureBox_Paint(object sender, PaintEventArgs e)
        {

            //Don't do anything if there is no picutre.
            if (imagePreviewPictureBox.Image != null)
            {

                //Make an array with 256 slots (or buckets)
                int[] histogram = new int[256];

                //Get the x and y dimensions of the image
                FrameDims framDims = theCamera.GetVideoFrameDimensions();
                double numPixelsCount;
                numPixelsCount = framDims.Width * framDims.Height;

                //Look at all pixels in the image. Keep a count of each 
                //pixel at a given  value. ie, if a pixel with the value 25 is found, 
                //go to array element 25, and add 1 to it. 
                for (int i = 0; i < framDims.Width; i++)
                    for (int j = 0; j < framDims.Height; j++)
                    {
                        byte pixelValue = theCamera.GetPixel(i, j);
                        histogram[pixelValue]++;
                    }

                int maxHist = getMaxArrayVal(histogram);

                //is Pallet Present
                if (maxHist < 25000)
                    isPalletPresent.Text = "Yes";
                else
                    isPalletPresent.Text = "No";

                //Do some scaling so that the output will look nice. 
                float scaling = (float)(histogramPictureBox.Height - 1) / getMaxArrayVal(histogram);

                for (int x = 0; x < 256; x++)
                {
                    Pen thePen = new Pen(Color.LimeGreen);
                    //When we draw, the coordiantes are taken from the top of the screen, so the "height - x" flips it around
                    //You can use these methods to draw lots of other things. Look up DrawRectange(), DrawText() and DrawElipse(). 
                    //these functions may be useful in your own projects. 
                    e.Graphics.DrawLine(thePen, x, histogramPictureBox.Height, x, histogramPictureBox.Height - (scaling * histogram[x]));
                }
            }

        }

        //256,113
        //740,113
        //center x pin 1 = 343; 2 = 421; 3 = 498; 4 = 575; 5 = 653
        // x distance between pins = 77 pixels
        // x distance between edge of pallet and first hole center = 87
        //center y pin 1 = 200; 2 = 277; 3 = 354...
        // y distance between pins = 77 pixels
        //

        //class PalletTopLeft
        //{
        //    int x;
        //    int y;
        //}

        //PalletTopLeft pallet;
        //PalletTopLeft.x = 256;
        //PalletTopLeft.y = 113;




        //Find the maximum value in an array
        private int getMaxArrayVal(int[] array)
        {
            int max = array[0];

            foreach (int val in array)
            {
                if (val > max)
                {
                    max = val;
                }
            }

            return max;

        }

        //Display a save dialog, save the image if it returns OK
        private void savePictureButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imagePreviewPictureBox.Image.Save(saveFileDialog1.FileName);
            }

        }

        //Send data out ot the Robot
        private void writeOutputButton_Click(object sender, EventArgs e)
        {
            IOCard.OpenIOCard();
            IOCard.DigitalOut((byte)writeOutputNumericUpDown.Value);
            IOCard.CloseIOCard();
        }


        //Get data in from the IO Card
        private void radInputButton_Click(object sender, EventArgs e)
        {
            IOCard.OpenIOCard();
            readInputlabel.Text = IOCard.DigitalIn().ToString();
            IOCard.CloseIOCard();
        }


        private BackgroundWorker VideoDisplayThread;
        private bool backgroundWorking = false;

        //You can safely ignore this function. It does the hard work to display the live
        //preview. Most solutions won't even need the live preview in the end. 
        private void displayLivePreviewCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (displayLivePreviewCheckBox.Checked)
            {
                //Conect to the camera 
                theCamera.CheckLink();
                theCamera.SelectCamera(0);
                theCamera.InitCamera(true);
                theCamera.SetShutterSpeed(3000);

                //Set the camera in aquisition mode
                theCamera.StartImageAcquisition();

                //Start a background thread to get images
                VideoDisplayThread = null;
                VideoDisplayThread = new BackgroundWorker();
                VideoDisplayThread.WorkerReportsProgress = true;
                VideoDisplayThread.DoWork += GenerateImages;
                VideoDisplayThread.ProgressChanged += UpdateGUI;
                VideoDisplayThread.RunWorkerAsync();

            }
        }


        //Grab frames from the camera in a background thread
        private void GenerateImages(object sender, DoWorkEventArgs e)
        {
            backgroundWorking = true;

            //While live preview is turned on, grab images and report them
            while (displayLivePreviewCheckBox.Checked)
            {
                theCamera.AcquireImage();
                VideoDisplayThread.ReportProgress(1, theCamera.GetBitmapImage());
            }

            //If the preview is turned off, stop getting images
            theCamera.StopImageAcquisition();

            //Tell the GUI thread that we have stopped
            backgroundWorking = false; ;
        }

        //Display images on the GUI
        private void UpdateGUI(object sender, ProgressChangedEventArgs e)
        {
            //Get the image from the background thread
            livePreviewPictureBox.Image = (Bitmap)(e.UserState);

            //Display it on the screen. (Same as redraw)
            livePreviewPictureBox.Invalidate();
        }

        //Make sure if we try to close, that we shut everything down neatly. 
        private void visionDemoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Shut down the Generate Images Function
            displayLivePreviewCheckBox.Checked = false;
            //Wait for it to finsish
            while (backgroundWorking)
            { }
        }

        private void PinCheckButton_Click(object sender, EventArgs e)
        {
            // Checks an individual slot for a pin
            if (PinCheck(Convert.ToInt32(PinCheckNumericUpDown.Value)))
                PinCheckResultLabel.Text = "Pin slot " + PinCheckNumericUpDown.Value.ToString() +
                                           " has a pin.";
            else
                PinCheckResultLabel.Text = "Pin slot " + PinCheckNumericUpDown.Value.ToString() +
                                           " does not have a pin.";
        }

        private void CheckAllPinsButton_Click(object sender, EventArgs e)
        {
            int PinCount = 0;

            // For each slot, check if pin is present, then check 
            // the corresponding checkbox in the display
            for (int i = 1; i < 26; i++)
            {
                Control c = this.Controls.Find("PinCheckBox" + i.ToString(), true)[0];
                (c as CheckBox).Checked = false;
                if (PinCheck(i))
                {
                    (c as CheckBox).Checked = true;
                    PinCount += 1;
                }
            }

            // Returns number of pins and empty slots
            numPins.Text = PinCount.ToString();
            numHoles.Text = (25 - PinCount).ToString();

        }

        private void imagePreviewPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Gets cursor position over the picture 
            // to set new pallet location
            PalletXPosTextBox.Text = (e.X).ToString();
            PalletYPosTextBox.Text = (e.Y).ToString();

            // Refresh position
            imagePreviewPictureBox.Refresh();
            // Check pins
            CheckAllPinsButton.PerformClick();
            // Refresh again to redraw
            imagePreviewPictureBox.Refresh();
        }

        private void PalletPosDefaultButton_Click(object sender, EventArgs e)
        {
            // Returns pallet location to default (256, 133)
            PalletXPosTextBox.Text = 256.ToString();
            PalletYPosTextBox.Text = 113.ToString();

            // Refresh position
            imagePreviewPictureBox.Refresh();
            // Check pins
            CheckAllPinsButton.PerformClick();
            // Refresh again to redraw
            imagePreviewPictureBox.Refresh();
        }

        private void imagePreviewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (true /*isPalletPresent.Text == "Yes"*/)
            {
                // Define location of the top left point of pallet
                int PalletXPos = Convert.ToInt32(PalletXPosTextBox.Text);
                int PalletYPos = Convert.ToInt32(PalletYPosTextBox.Text);
                int PalletWidth = 484;

                // Drawing rectangle around pallet
                e.Graphics.DrawLine(new Pen(Color.Red, 1f),
                    new Point(PalletXPos - 5, PalletYPos),
                    new Point(PalletXPos, PalletYPos));

                e.Graphics.DrawLine(new Pen(Color.Red, 1f),
                    new Point(PalletXPos, PalletYPos - 5),
                    new Point(PalletXPos, PalletYPos));

                e.Graphics.DrawRectangle(new Pen(Color.Red, 1f),
                    PalletXPos, PalletYPos, PalletWidth, PalletWidth);

                // Drawing circles and crosshairs at pin points 
                const int palletborder = 10, PinSpacing = 77, PinRadius = 22;

                int PinCenX, PinCenY;
                Color NoPinColour = Color.Red;
                Color YesPinColour = Color.Green;
                Color PinColour;
                Control c;
                int PinX, PinY;
                for (int i = 1; i < 26; i++)
                {
                    PinY = i / 5 + 1;
                    PinX = i % 5;
                    if (PinX == 0) { PinX = 5; PinY -= 1; }

                    PinCenX = Convert.ToInt32(PalletXPosTextBox.Text) +
                                    palletborder + PinX * PinSpacing;
                    PinCenY = Convert.ToInt32(PalletYPosTextBox.Text) +
                                    palletborder + PinY * PinSpacing;

                    c = this.Controls.Find("PinCheckBox" + i.ToString(), true)[0];
                    if ((c as CheckBox).Checked) PinColour = YesPinColour;
                    else PinColour = NoPinColour;


                    e.Graphics.DrawLine(
                    new Pen(PinColour, 1f),
                    new Point(PinCenX - 5, PinCenY),
                    new Point(PinCenX + 5, PinCenY));

                    e.Graphics.DrawLine(
                    new Pen(PinColour, 1f),
                    new Point(PinCenX, PinCenY - 5),
                    new Point(PinCenX, PinCenY + 5));

                    e.Graphics.DrawEllipse(
                    new Pen(PinColour, 2f), PinCenX - PinRadius, 
                    PinCenY - PinRadius, PinRadius * 2, PinRadius * 2);
                }
            }
        }

    }
}