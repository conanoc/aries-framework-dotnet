using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;

namespace WebAgent.Protocols.BasicMessage
{
    public class DefaultProofHandler : IMessageHandler
    {
        private readonly IProofService _proofService;

        public DefaultProofHandler(IProofService proofService)
        {
            _proofService = proofService;
        }

        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            MessageTypes.PresentProofNames.Presentation,
            MessageTypes.PresentProofNames.RequestPresentation
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AriesFrameworkException">Unsupported message type {messageType}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                // v1.0
                case MessageTypes.PresentProofNames.RequestPresentation:
                {
                    var message = messageContext.GetMessage<RequestPresentationMessage>();
                    var record = await _proofService.ProcessRequestAsync(agentContext, message, messageContext.Connection);

                    messageContext.ContextRecord = record;
                    Console.WriteLine("Received Request ===============");
                    Console.WriteLine(record.RequestJson);
                    var requestedCredentials = new RequestedCredentials();
                    var request = JsonConvert.DeserializeObject<ProofRequest>(record.RequestJson);
                    var credentials = await _proofService.ListCredentialsForProofRequestAsync(agentContext, request, "names-requirement");
                    if (credentials.Count() == 0)
                    {
                        Console.WriteLine("Cannot find any credentials to prove !!!!!!!!!!!!!!");
                        return null;
                    }
                    requestedCredentials.RequestedAttributes.Add("names-requirement",
                        new RequestedAttribute
                        {
                            CredentialId = credentials.First().CredentialInfo.Referent,
                            Revealed = true
                        });
                    Console.WriteLine("Sending proof message...");
                    var (proofMsg, _) = await _proofService.CreatePresentationAsync(agentContext, record.Id, requestedCredentials);
                    return proofMsg;
                }
                case MessageTypes.PresentProofNames.Presentation:
                {
                    var message = messageContext.GetMessage<PresentationMessage>();
                    var record = await _proofService.ProcessPresentationAsync(agentContext, message);

                    messageContext.ContextRecord = record;
                    Console.WriteLine("Received Proof ===============");
                    Console.WriteLine(record);
                    break;
                }
            }
            return null;
        }
    }
}
