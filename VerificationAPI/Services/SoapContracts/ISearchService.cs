using CoreWCF;
using System.ServiceModel;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Services.SoapContracts
{
    [ServiceContract]
    public interface ISearchService
    {
        [OperationContract]
        List<VideoMetadata> Search(string term);
    }
}
