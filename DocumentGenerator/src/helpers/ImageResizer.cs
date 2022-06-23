using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DocumentGenerator.Helpers {
    public static class ImageResizer {

        public static byte[] resize(byte[] bytes, int _width, int _height) {
            if (bytes.Length > 0) {
                using (var ms = new MemoryStream(bytes)) {
                    var image = Image.FromStream(ms);

                    var ratioX = (double)_width / image.Width;
                    var ratioY = (double)_height / image.Height;
                    var ratio = Math.Min(ratioX, ratioY);

                    var width = (int)(image.Width * ratio);
                    var height = (int)(image.Height * ratio);

                    var newImage = new Bitmap(374, 181);
                    Graphics.FromImage(newImage).DrawImage(image, 0, 0, width, height);
                    Bitmap bmp = new Bitmap(newImage);

                    ImageConverter converter = new ImageConverter();

                    bytes = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
                }
            } 
            
            return bytes;
        
        }
    }
}
