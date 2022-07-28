using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

// From https://github.com/cmorgado/MultiPlatform/blob/master/src/MultiPlatform.W81.UI/Services/UiStorage.cs

namespace MultiPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                UiStorage uUiStorage = new UiStorage();

                uUiStorage.CopyFile(args[0],args[1], false).Wait();
            }
        }

        private class UiStorage 
        {

            private static readonly StorageFolder Storage = ApplicationData.Current.LocalFolder;

            public async Task CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
            {
                StorageFile file;
                file = await Storage.GetFileAsync(sourceFileName);

                string destinationFolderName;
                destinationFolderName = Path.GetDirectoryName(destinationFileName);

                StorageFolder destinationFolder;
                destinationFolder = await Storage.GetFolderAsync(destinationFolderName);


                destinationFileName = Path.GetFileName(destinationFileName);

                var nameCollisionOption = overwrite ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists;

                StorageFolder destinationFolder1;
                destinationFolder1 = destinationFolder;

                StorageFile file1;
                file1 = file;

                await file1.CopyAsync(destinationFolder1, destinationFileName, nameCollisionOption);
            }
        }
    }
}
