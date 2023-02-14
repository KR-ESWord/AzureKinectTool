using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Azure Kinect SDK
using Microsoft.Azure.Kinect.Sensor;

namespace AzureKinectTool.function
{
    public class AKImageConvert
    {
        // Azure Kinect SDK의 Image의 Color 객체를 Writeablebitmap 객체로 변환화는 과정
        public WriteableBitmap ColorConvert(Image color_image)
        {
            int width = color_image.WidthPixels;
            int height = color_image.HeightPixels;
            
            WriteableBitmap color_wbitmap = null;
            byte[] color_buffer = null;

            Parallel.Invoke(new Action(delegate {
                color_buffer = color_image.Memory.ToArray();

                color_wbitmap = new WriteableBitmap(
                    width, height,
                    96.0, 96.0,
                    PixelFormats.Bgra32, null
                    );
            }));

            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = width * sizeof(byte) * 4;
            color_wbitmap.WritePixels(rect, color_buffer, stride, 0, 0);
            color_wbitmap.Freeze();

            return color_wbitmap;
        }

        // Azure Kinect SDK의 Image의 Depth 객체를 Writeablebitmap 객체로 변환화는 과정
        public WriteableBitmap DepthConvert(Image depth_image)
        {
            int width = depth_image.WidthPixels;
            int height = depth_image.HeightPixels;

            WriteableBitmap depth_wbitmap = null;
            byte[] depth_buffer = null;

            Parallel.Invoke(new Action(delegate {
                depth_buffer = depth_image.Memory.ToArray();

                depth_wbitmap = new WriteableBitmap(
                    width, height,
                    96.0, 96.0,
                    PixelFormats.Gray16, null
                    );
            }));

            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = width * sizeof(byte) * 2;
            depth_wbitmap.WritePixels(rect, depth_buffer, stride, 0, 0);
            depth_wbitmap.Freeze();

            return depth_wbitmap;
        }

        // Azure Kinect SDK의 Image의 Transformed Depth 객체 생성 후 Writeablebitmap 객체로 변환화는 과정
        public WriteableBitmap TrDepthConvert(Transformation transformation, Image depth_image)
        {
            // 일반 Depth Image 객체를 transformation 함수를 이용하여 Transformed Depth Image 객체 생성
            Image tr_depth_image = transformation.DepthImageToColorCamera(depth_image);

            int width = tr_depth_image.WidthPixels;
            int height = tr_depth_image.HeightPixels;

            WriteableBitmap trdepth_wbitmap = null;
            byte[] trdepth_buffer = null;

            Parallel.Invoke(new Action(delegate {
                trdepth_buffer = tr_depth_image.Memory.ToArray();

                trdepth_wbitmap = new WriteableBitmap(
                    width, height,
                    96.0, 96.0,
                    PixelFormats.Gray16, null
                    );
            }));

            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = width * sizeof(byte) * 2;
            trdepth_wbitmap.WritePixels(rect, trdepth_buffer, stride, 0, 0);
            trdepth_wbitmap.Freeze();

            return trdepth_wbitmap;
        }

        // Azure Kinect SDK의 Image의 IR 객체를 Writeablebitmap 객체로 변환화는 과정
        public WriteableBitmap IRConvert(Image ir_image)
        {
            int width = ir_image.WidthPixels;
            int height = ir_image.HeightPixels;

            WriteableBitmap ir_wbitmap = null;
            byte[] ir_buffer = null;

            Parallel.Invoke(new Action(delegate {
                ir_buffer = ir_image.Memory.ToArray();

                ir_wbitmap = new WriteableBitmap(
                    width, height,
                    96.0, 96.0,
                    PixelFormats.Gray16, null
                    );
            }));

            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = width * sizeof(byte) * 2;
            ir_wbitmap.WritePixels(rect, ir_buffer, stride, 0, 0);
            ir_wbitmap.Freeze();

            return ir_wbitmap;
        }
    }
}
