using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.Services
{
    public class AzureBlobStorageSilo : ISilo
    {
        private readonly CloudBlobContainer _container;

        public StorageCredentials Credentials { get; set; }

        public AzureBlobStorageSilo(StorageCredentials credentials, string name)
        {
            var uri = new Uri("http://" + credentials.AccountName + ".blob.core.windows.net/");
            var client = new CloudBlobClient(uri, credentials);
            _container = client.GetContainerReference(name);
        }

        public async Task<bool> Store(Guid signature, byte[] buffer, string mime = null)
        {
            var blob = _container.GetBlockBlobReference(signature.ToString());
            try
            {
                if (mime != null)
                    blob.Properties.ContentType = mime;

                await blob.UploadFromByteArrayAsync(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception)
            {
                if (this.IsDebug())
                    throw;
                return false;
            }
        }

        public async Task<byte[]> Retrieve(Guid signature)
        {
            var blob = _container.GetBlockBlobReference(signature.ToString());
            try
            {
                using (var stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);
                    return stream.ToArray();
                }
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    return null;

                throw;
            }
        }

        public async Task DeleteIfExists(Guid signature)
        {
            var blob = _container.GetBlockBlobReference(signature.ToString());
            await blob.DeleteIfExistsAsync();
        }

    }
}
