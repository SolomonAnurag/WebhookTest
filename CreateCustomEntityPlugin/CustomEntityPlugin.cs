using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;


namespace CreateCustomEntityPlugin
{
    public class CustomEntityPlugin : IPlugin
    {

        public void Execute(IServiceProvider serviceProvider)
        {

            DateTime postCreatedon = new DateTime();
            Guid individualID = new Guid();
            Guid latestDonationID = new Guid();
            Money donationAmount = new Money();
            OptionSetValue impactReport = new OptionSetValue();
            //var IMPACTREPORT = "";

            ITracingService tracingService =
               (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Register this plug -in on the Create message, test_Donation entity in asynchronous mode.
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetdonation = (Entity)context.InputParameters["Target"];
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = factory.CreateOrganizationService(context.UserId);

                // get PostImage from Context
                if (context.PostEntityImages.Contains("PostImage") &&
                   context.PostEntityImages["PostImage"] is Entity)
                {
                    Entity postMessageImage = (Entity)context.PostEntityImages["PostImage"];
                    // Get the field value after database update performed

                    postCreatedon = (DateTime)postMessageImage.Attributes["createdon"];
                    individualID = ((Microsoft.Xrm.Sdk.EntityReference)postMessageImage.Attributes["test_individualdonationsid"]).Id;
                    latestDonationID = new Guid(postMessageImage.Attributes["test_donationid"].ToString());
                    donationAmount = (Money)postMessageImage.Attributes["test_donationamount"];
                    impactReport = postMessageImage.GetAttributeValue<OptionSetValue>("test_imapctreport");
                    // IMPACTREPORT = impactReport.ToString();



                    service.Update(targetdonation);

                    //orgcontext = new OrganizationServiceContext(service);
                    //orgcontext.AddObject(targetdonation);
                    //orgcontext.SaveChanges();

                }

                // Retrieve the latest Donation records for an Individual.     
                QueryExpression IndividualDonationsQuery = new QueryExpression
                {
                    EntityName = "test_donation",
                    ColumnSet = new ColumnSet("test_individualdonationsid", "test_latestdate", "createdon", "test_donationamount", "test_impactreport"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                            {
                                new ConditionExpression
                                {
                                    AttributeName = "test_individualdonationsid",
                                    Operator = ConditionOperator.Equal,
                                    Values = { individualID.ToString() }
                                }
                            }
                    }
                };

                // Get latest Record - Donation
                IndividualDonationsQuery.Orders.Add(new OrderExpression("createdon", OrderType.Descending));
                DataCollection<Entity> test_donation = service.RetrieveMultiple(IndividualDonationsQuery).Entities;



                QueryExpression getguid = new QueryExpression()

                {

                    EntityName = "test_customentity",

                    Criteria ={

                                        Conditions =

                                        {

                                        new ConditionExpression("test_individualname",ConditionOperator.Equal,individualID),

                                        },

                                        }

                };

                EntityCollection retrieveCustonEntityguid = service.RetrieveMultiple(getguid);

                if (retrieveCustonEntityguid.Entities.Count > 0)

                {

                    Entity UpdateCustomEntity = new Entity("test_customentity");
                    UpdateCustomEntity = retrieveCustonEntityguid.Entities[0];

                    Guid Id = retrieveCustonEntityguid.Entities[0].Id;
                    UpdateCustomEntity.Id = Id;
                    UpdateCustomEntity["test_donationlatestdate"] = postCreatedon;
                    EntityReference donationReference = new EntityReference("test_donation", latestDonationID);
                    UpdateCustomEntity["test_latestdonationname"] = donationReference;
                    UpdateCustomEntity["test_amount"] = donationAmount;
                    UpdateCustomEntity["test_reportstatus"] = impactReport;

                    // Update the custom Entity with the latest details
                    service.Update(UpdateCustomEntity);

                }
                else
                {
                    try
                    {
                        Entity newCustomEntity = new Entity("test_customentity");
                        newCustomEntity["test_name"] = "TEST";
                        newCustomEntity["test_donationlatestdate"] = postCreatedon;
                        newCustomEntity["test_individualname"] = new EntityReference("contact", individualID);

                        EntityReference customdonationReference = new EntityReference("test_donation", latestDonationID);
                        newCustomEntity["test_latestdonationname"] = customdonationReference;
                        newCustomEntity["test_amount"] = donationAmount;
                        newCustomEntity["test_reportstatus"] = impactReport;

                        service.Create(newCustomEntity); // to create customEntity

                    }
                    catch (System.ServiceModel.FaultException<OrganizationServiceFault> ex)
                    {
                        throw new InvalidPluginExecutionException("An error occurred in the FollowupPlugin plug-in.", ex);
                    }

                    catch (Exception ex)
                    {
                        tracingService.Trace("createPlugin: {0}", ex.ToString());
                        throw;
                    }
                }
                if (context.Depth > 1)
                {
                    return;
                }


            }

        }
    }
}
