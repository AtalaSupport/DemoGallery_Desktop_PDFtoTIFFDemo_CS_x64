using System;
using System.IO;
using Atalasoft.Imaging;
using Atalasoft.Imaging.Codec;
using Atalasoft.Imaging.Codec.Pdf;
using System.Windows.Forms;
using Atalasoft.Imaging.ImageSources;


namespace PDFtoTIFF
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// Converts any multipage PDF file into a multipage TIFF file
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
            DoAboutSplash();

            Console.WriteLine("PdfToTiffConverter starting");

            //// Defining a PDF decoder that we will add to our registered decoders
            PdfDecoder pdfDec = new PdfDecoder();
            //// Setting the resolution to 200DPI which is common for TIFF files
            pdfDec.Resolution = 200;
            //// Adding the decoder to our registed decoders makes handling of PDFs automatic
            RegisteredDecoders.Decoders.Add(pdfDec);

			string imgPath = GetWorkingDir();
            string inFile = imgPath + "target.pdf";

            Console.WriteLine("Enter the PDF File to convert to TIFF:");

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.InitialDirectory = imgPath;
                dlg.Filter = "Portable Document Format (*.pdf)|*.pdf;|All Files (*.*)|*.*";
                dlg.FileName = Path.GetFileName(inFile);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    inFile = dlg.FileName;
                }
                else
                {
                    inFile = null;
                }
            }

            if (inFile == null)
            {
                Console.WriteLine("No input file SelectedOperation canceled");
            }
            else
            {

                string outFile = System.IO.Path.ChangeExtension(inFile, ".tif");

                Console.WriteLine("  inFile: " + inFile);

                Console.WriteLine("Please select a location to save the outgoing TIFF to:");
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = "Select Location to Save output TIFF to";
                    dlg.Filter = "Portable Document Format (.tif)|.tif";
                    dlg.DefaultExt = ".tif";
                    dlg.InitialDirectory = Path.GetDirectoryName(inFile);
                    dlg.FileName = Path.GetFileName(outFile);

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        outFile = dlg.FileName;
                    }
                    else
                    {
                        // force an error to stop the app
                        outFile = null;
                    }
                }

                Console.WriteLine("  outFile: " + outFile);

                if (outFile == null)
                {
                    Console.WriteLine("No Output selcted: Operation canceled");
                }
                else
                {
                    Console.WriteLine("Converting file...");

                    // start timer
                    int tick1 = System.Environment.TickCount;

                    // Do the conversion
                    if (File.Exists(inFile))
                    {
                        ConvertPdfToTiff(inFile, outFile);
                    }
                    else
                    {
                        Console.WriteLine("file not found... doing nothing");
                    }
                    // finish timer
                    int tick4 = System.Environment.TickCount;

                    Console.WriteLine("Conversion complete: Total time " + (tick4 - tick1) + " ms");
                }
            }

            Console.WriteLine("PdfToTiffConverter finished... press ENTER to quit");
			Console.ReadLine();
		}


        /// <summary>
        /// Given the filename of a tiff file as input and a pdf file as output, convert the
        /// incoming tiff to a SingleImageOnly PDF
        /// 
        /// This demonstrates the most memory-efficient way to do the conversion.
        /// </summary>
        /// <param name="inFile">A tiff file to use as the input</param>
        /// <param name="outFile">A filename to call the outgoing PDF (note that it will delete any existing file by that name first.</param>
        private static void ConvertPdfToTiff(string inFile, string outFile)
        {
            // Make sure we get rid of the output file if it already exists
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }

            // Create your encoder
            TiffEncoder tiffEnc = new TiffEncoder();

            // For Jpeg compression (not Jpeg2000), you can use this value to request 
            // higher compression or quality... the higher the number, the better the 
            // quality, but the less effective the compression. 80 is the default value/
            tiffEnc.JpegQuality = 75;

            // set up an event handler that will pop for each image to let you determine the compression
            tiffEnc.SetEncoderCompression += new EncoderCompressionEventHandler(tiffEnc_SetEncoderCompression);

            using (FileStream inStream = new FileStream(inFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (PdfImageSource imgSrc = new PdfImageSource(inStream) { Resolution = 200, RenderSettings = new RenderSettings(){  AnnotationSettings = AnnotationRenderSettings.RenderNone } })
                {
                    using (FileStream outStream = new FileStream(outFile, FileMode.Create))
                    {
                        // The magic of using an ImageSource and Stream with the pdfEncoder is 
                        // that only the active portion of a given image is in memory at one time
                        // so it's very memory efficient.
                        //
                        // This effectively does   while(imgSrc.HasMoreImages()) { AtalaImage img = imgSrc.AcquireNext(); ... }
                        //
                        // Each page willl trigger a new tiffEnc_SetEncoderCompression event 
                        // where you can choose the type of compression you want to apply on a page-by-page basis

                        tiffEnc.Save(outStream, imgSrc, null);
                    }
                }
            }

            /* *****************************************
             * SUPPORT NOTE::
             * We've modified this demo to use our PdfImageSource instead of our FileSystemImageSource as it does a better job handling PDFs
             * the original code to convert using the FileSystemImageSource is commented out below
             * 
             */
            //// Reading this direct from a FileSystemImageSource will be much more efficient than using an ImageCollection
            //using (ImageSource imgSrc = new FileSystemImageSource(inFile, true))
            //{
            //    using (Stream s = File.OpenWrite(outFile))
            //    {
            //        // The magic of using an ImageSource and Stream with the pdfEncoder is 
            //        // that only the active portion of a given image is in memory at one time
            //        // so it's very memory efficient.
            //        //
            //        // This effectively does   while(imgSrc.HasMoreImages()) { AtalaImage img = imgSrc.AcquireNext(); ... }
            //        //
            //        // Each page willl trigger a new tiffEnc_SetEncoderCompression event 
            //        // where you can choose the type of compression you want to apply on a page-by-page basis
            //        tiffEnc.Save(s, imgSrc, null);
            //    }
            //}
            // just being polite and cleaning up after ourselves
            tiffEnc = null;
        }


        /// <summary>
        /// Intelligently selects the appropriate form of compression to apply to the current page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void tiffEnc_SetEncoderCompression(object sender, EncoderCompressionEventArgs e)
        {
            if (e.Image.PixelFormat == PixelFormat.Pixel1bppIndexed)
            {
                // Here, we've chosen Group4FaxEncoding as our B&W conversion.
                e.Compression = new TiffCodecCompression(TiffCompression.Group4FaxEncoding);
                // Group3FaxEncoding may be a viable alternative for you
                //e.Compression = new TiffCodecCompression(TiffCompression.Group3FaxEncoding);
                // ModifiedHuffman may be a viable alternative for you
                //e.Compression = new TiffCodecCompression(TiffCompression.ModifiedHuffman);
            }
            else if (e.Image.PixelFormat == PixelFormat.Pixel8bppGrayscale)
            {
                // LZW may do a better job on Graysale than Jpeg Compression, and without the resulting loss
                e.Compression = new TiffCodecCompression(TiffCompression.Lzw);
            }
            else if (e.Image.PixelFormat == PixelFormat.Pixel24bppBgr)
            {
                // Here, we've chosen JpegCompression as our Color conversion.
                e.Compression = new TiffCodecCompression(TiffCompression.JpegCompression);
                // If quality is much more important than compression, you could use LZW or Deflate here
                //e.Compression = new TiffCodecCompression(TiffCompression.Lzw);
                //e.Compression = new TiffCodecCompression(TiffCompression.Deflate);
            }
            else
            {
                // Fallback method in case the pixelFormat isn't one of the ones defined above
                e.Compression = new TiffCodecCompression(TiffCompression.Deflate);
                // LZW may be a viable alternative for you
                //e.Compression = new TiffCodecCompression(TiffCompression.Lzw);
            }
        }

        /// <summary>
        /// Convenience method to get the root directory of the project - really only useful for debugging
        /// </summary>
        /// <returns></returns>
        private static string GetWorkingDir()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            //Console.WriteLine("cwd is '{0}'", cwd);

            if (cwd.EndsWith("\\bin\\Debug"))
            {
                cwd = cwd.Replace("\\bin\\Debug", "\\..\\..\\");
                //Console.WriteLine("updated cwd is '{0}'", cwd);
            }
            else if (cwd.EndsWith("\\bin"))
            {
                cwd = cwd.Replace("\\bin", "\\..\\");
                //Console.WriteLine("updated cwd is '{0}'", cwd);
            }
            return cwd;
        }

        /// <summary>
        /// Outputs the "About" spash info
        /// </summary>
        private static void DoAboutSplash()
        {
            Console.WriteLine("PDFtoTIFF Demo");
            Console.WriteLine();
            Console.WriteLine("***************************************************************************");
            Console.WriteLine("A very simple console app that converts a PDF file into a TIFF by");
            Console.WriteLine("using in a memory-efficient way using PdfImageSource (part of our ");
            Console.WriteLine("PdfReader addon.");
            Console.WriteLine();
            Console.WriteLine("Who says you always need a viewer in an imaging application?");
            Console.WriteLine();
            Console.WriteLine("This console app uses our PdfImageSource to convert a PDF to a TIFF.");
            Console.WriteLine("This approach can easily be adapted to services or plumbed in to");
            Console.WriteLine("batch-based processing.");
            Console.WriteLine();
            Console.WriteLine("By setting a handler for TiffEncoder.SetEncoderCompression, we are ");
            Console.WriteLine("able to dynamically select the most appropriate form of image ");
            Console.WriteLine("compression to apply, based on the PixelFormat (color depth) of each page");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Download the DotImage SDK at:");
            Console.WriteLine("     http://www.atalasoft.com/products/download/dotimage");
            Console.WriteLine();
            Console.WriteLine("Download the DotImage API Help Installers at:");
            Console.WriteLine("     http://www.atalasoft.com/support/dotimage/help/install");
            Console.WriteLine();
            Console.WriteLine("Download the full sources for this demo at:");
            Console.WriteLine("     http://www.atalasoft.com/KB/article.aspx?id=10412");
            Console.WriteLine("***************************************************************************");
            Console.WriteLine();
        }
	}
}
