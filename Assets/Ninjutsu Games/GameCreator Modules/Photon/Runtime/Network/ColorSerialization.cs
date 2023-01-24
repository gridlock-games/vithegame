namespace NJG.PUN
{
    public class ColorSerialization
    {
        public byte Id { get; set; }

        public static object Deserialize(byte[] data)
        {
            var result = new ColorSerialization();
            result.Id = data[0];
            return result;
        }

        public static byte[] Serialize(object customType)
        {
            var c = (ColorSerialization)customType;
            return new byte[] { c.Id };
        }
    }
}