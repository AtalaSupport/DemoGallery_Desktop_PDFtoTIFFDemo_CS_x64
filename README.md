# PDFtoTIFFDemo
A very simple console app that converts a PDF file into a TIFF by using in a 
memory-efficient way using PdfImageSOurce (part of our PdfReader addon.

Who says you always need a viewer in an imaging application?

This console app uses our PdfRasterizer to convert a PDF to a TIFF.

This approach can easily be adapted to services or plumbed in to batch-based 
processing.

By setting a handler for TiffEncoder.SetEncoderCompression, we are able to 
dynamically select the most appropriate form of image compression to apply, 
based on the PixelFormat (color depth) of each page


This is the C# version

## Instructions
After installing and activating the SDK, you can unzip, build and run this 
solution. It will prompt you for a PDF File with an OpenFileDialog.

Select the PDF you want to convert and open. It will then prompt you where 
to save the TIFF and for filename. The file will be converted and saved.

## Prerequisites
This demo assumes you have the Atalasoft DotImage SDK installed and 
licensed for DotImage Document Imaging and PdfReader add-on (or you can 
request a 30 day evaluation when installing/activating)

[Download DotImage](https://www.atalasoft.com/BeginDownload/DotImageDownloadPage)

## Cloning
We recommend the following if you wish to donload/clone a copy

Example: git for windows
```bash
git clone https://github.com/AtalaSupport/DemoGallery_Desktop_PDFtoTIFFDemo_CS_x64.git PDFtoTIFFDemo
```

## Last Update
Last updated 2025-11-17 - TD

