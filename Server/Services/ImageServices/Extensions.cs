namespace Viewer.Server.Services.ImageServices;

public static class Extensions
{
    public static void ResizeImage(this Image img, int w, int h)
    {
        if (w < 1 || h < 1)
        {
            if (w == img.Width && h == img.Height)
            {
                return;
            }
            if (w > 0 && h > 0)
            {
                img.Mutate(x => x.Resize(w, h));
            }
            else if (w > 0)
            {
                if (h < 1)
                    h = Convert.ToInt32(img.Height / (float)img.Width * w);
                img.Mutate(x => x.Resize(w, h));
            }
            else if (h > 0)
            {
                if (w < 1)
                    w = Convert.ToInt32(img.Width / (float)img.Height * h);
                img.Mutate(x => x.Resize(w, h));
            }
            else
            {
                return;
            }
        }
    }
}
