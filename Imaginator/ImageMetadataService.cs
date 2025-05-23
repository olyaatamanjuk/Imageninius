namespace Imaginator;

using ImageMagick;

public class ImageMetadataService
{
    public byte[] AddIptcMetadata(byte[] imageBytes, string headline, string caption, string keywordsCsv)
    {
        using var image = new MagickImage(imageBytes);

        var iptcProfile = image.GetIptcProfile() ?? new IptcProfile();

        // Видаляємо існуючі теги
        iptcProfile.RemoveValue(IptcTag.Headline);
        iptcProfile.RemoveValue(IptcTag.Caption);
        iptcProfile.RemoveValue(IptcTag.Keyword);

        // Додаємо нові значення
        iptcProfile.SetValue(IptcTag.Headline, headline);
        iptcProfile.SetValue(IptcTag.Caption, caption);

        foreach (var keyword in keywordsCsv.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)))
        {
            iptcProfile.SetValue(IptcTag.Keyword, keyword);
        }

        image.SetProfile(iptcProfile);
    
        return image.ToByteArray(image.Format);
    }
}