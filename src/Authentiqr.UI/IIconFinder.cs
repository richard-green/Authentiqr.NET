using System.Drawing;

namespace Authentiqr.NET
{
    public interface IIconFinder
    {
        Image FindImage(string accountName);
    }
}
