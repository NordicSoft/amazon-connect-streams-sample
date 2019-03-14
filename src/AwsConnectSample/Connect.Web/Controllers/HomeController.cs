using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;
using Amazon;
using Amazon.Connect;
using Amazon.Connect.Model;
using Connect.Web.Core;

namespace Connect.Web.Controllers
{
    public class HomeController : Core.ControllerBase
    {
        public ActionResult Index()
        {
            return View();
        }

        [AjaxOnly]
        [HttpPost]
        public ActionResult InitOutboundCall(string number, string idContactFlow, string sourcePhoneNumber)
        {

            string accessKey = WebConfigurationManager.AppSettings["AWSAccessKey"];
            string secretKey = WebConfigurationManager.AppSettings["AWSSecretKey"];
            AmazonConnectClient client = new AmazonConnectClient(accessKey, secretKey, RegionEndpoint.USEast1);
            var request = new StartOutboundVoiceContactRequest
            {
                InstanceId = "10a4c4eb-f57e-4d4c-b602-bf39176ced07", //The identifier for your Amazon Connect instance. To find the ID of your instance,
                                                                     //open the AWS console and select Amazon Connect. Select the alias of the instance in the Instance alias column.
                                                                     //The instance ID is displayed in the Overview section of your instance settings.
                                                                     //For example, the instance ID is the set of characters at the end of the instance ARN, after instance/, such as 10a4c4eb-f57e-4d4c-b602-bf39176ced07.
                SourcePhoneNumber = sourcePhoneNumber, //one of нour numbers 
                ContactFlowId = idContactFlow, 
                DestinationPhoneNumber = number,
                Attributes = new Dictionary<string, string>()//variables for text at contactFlow
                {
                    { "agentName", "Alex" },
                    { "contactFlowId", idContactFlow },
                    { "orderNumber", "AB123456" },
                    { "notifyUrl", "http://this.hostname/Home/ProcessLogs" } //Route to ProcessLogs action
                } 
            };
            try
            {

                var response = client.StartOutboundVoiceContact(request); 
                return new JsonNetResult(new JsonResponse(true, response.ContactId));
            }
            catch (Amazon.Connect.AmazonConnectException ex)
            {
                Logger.Error(ex);
                return new JsonNetResult(new JsonResponse(false, ex));
            }
        }

        [AjaxOnly]
        [HttpPost]
        public ActionResult StopOutboundCall(string contactId)
        {
            string accessKey = WebConfigurationManager.AppSettings["AWSAccessKey"];
            string secretKey = WebConfigurationManager.AppSettings["AWSSecretKey"];
            AmazonConnectClient client = new AmazonConnectClient(accessKey, secretKey, RegionEndpoint.USEast1);
            var stopRequest = new StopContactRequest
            {
                ContactId = contactId,//The unique identifier of the contact to end.
                InstanceId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" //The identifier for your Amazon Connect instance
            };

            try
            {
                client.StopContact(stopRequest);
            }
            catch (Amazon.Connect.AmazonConnectException ex)
            {
                Logger.Error(ex);
                return new JsonNetResult(new JsonResponse(false, ex));
            }

            return new JsonNetResult(new JsonResponse(true));
        }

        [HttpPost]
        public ActionResult ProcessLogs()
        {

            using (Stream receiveStream = Request.InputStream)
            {
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    var stringLogs = readStream.ReadToEnd();

                    Logger.Info("info log");
                    Logger.Info(stringLogs);
                    //LogExample
                    /*{
                        attributes: {
                            TestAttribute: 'TestValue',
                            agentName: 'Alex',
                            isConfirmed: 'No',
                            orderNumber: 'AB123456'
                        },
                        contactId: '00707a51-5a01-4bdd-8c43-19519ed0f2dc',
                        initiationTimestamp: '2018-05-03T15:34:12Z',
                        systemEndpoint: {
                            Address: '+12345678912',
                            Type: 'TELEPHONE_NUMBER'
                        },
                        CustomerEndpoint: {
                            Address: '+12345678912',
                            Type: 'TELEPHONE_NUMBER'
                        },
                        disconnectTimestamp: '2018-05-03T15:34:44Z',
                        duration: '0:32',
                        recording: null,
                        contactFlowId: 'ba81ff3e-2a88-4eb7-81ab-f544f4a0ed63',
                        settedAttributes: {
                            isConfirmed: 'No',
                            TestAttribute: 'TestValue'
                        },
                        isContactFlowEnded: true
                    }*/

                }
            }


            return new JsonNetResult(new JsonResponse(true));

        }
    }
}