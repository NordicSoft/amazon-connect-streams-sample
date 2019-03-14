var AWS = require('aws-sdk'),
  http = require('http'),
  docClient = new AWS.DynamoDB.DocumentClient({ region: 'us-east-1' }),
  url = require('url');

const contactFlows = { // for all flows mandatory attributes: operatorId, writerId, orderId. It depends on entities and DB schema. 
  "12345678-1234-1234-1234-123456789123": "Test outbound contact Flow", //none additional attributes
  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx": "Outbound contact Flow 2", //agentName, siteDomain, orderId
  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx": "Intbound contact Flow 3" //agentName, siteDomain, orderId
};

const callStatuses = {
  NotAnswered: "Not Answered",
  AnsweredNotListened: "Answered but not listened",
  AnsweredListenedNotConfirmed: "Answered, listened, not confirmed",
  AnsweredListenedConfirmed: "Answered, listened, confirmed"
}

const hostOptions = {
  hostname: "yourhostname", //change this
  port: "8000",
  path: "/home/connect"
};

exports.handler = function (event, context) {
  console.log("current data:", new Date())
  console.log('Received event:', JSON.stringify(event, null, 2));

  event.Records.forEach((record) => {
    // Kinesis data is base64 encoded so decode here
    const data = new Buffer(record.kinesis.data, 'base64').toString('ascii');

    var dataObj = JSON.parse(data);
    if (dataObj["EventType"] === "HEART_BEAT") {
      console.log("HEART_BEAT");
      context.done();
      return;
    }
    if (!dataObj.EventId && dataObj.ContactId) {
      console.log("item data:", data);
      var kinesisPromise = processingPromise(dataObj);

      var getItemParams = {
        Key: { ContactId: dataObj.ContactId },
        TableName: "tempLogs"
      };
      if (!getItemParams)
        throw new Error('An error occurred. getItemParams is undefined')

      var dynamoPromise = getDynamoDbPromise(docClient, getItemParams)
        .then(onFetchEventsSuccess)
        .then(processDynamoItem);

      Promise.all([dynamoPromise, kinesisPromise]).then(results => {
        return mergeDynamoKinesis(results[0], results[1]);
      }).then(postDetailsToHost)
        .then(() => { context.done(null, 'processed logs are sent to Host'); })
        .catch((err) => { console.log("error ", err, err.stack); context.fail(err); });
    }
  });
};

function millisToMinutesAndSeconds(millis) {
  var minutes = Math.floor(millis / 60000);
  var seconds = ((millis % 60000) / 1000).toFixed(0);
  return minutes + ":" + (seconds < 10 ? '0' : '') + seconds;
}

function processKinesisEvent(kinesisEvent, callback) {
  try {
    var result = {};
    result.callStatus = kinesisEvent.ConnectedToSystemTimestamp == null ? callStatuses.NotAnswered :
                        kinesisEvent.Attributes.isConfirmed != null ? callStatuses.AnsweredListenedConfirmed : callStatuses.AnsweredNotListened;
    result.attributes = kinesisEvent.Attributes;
    result.contactId = kinesisEvent.ContactId;
    result.initiationTimestamp = kinesisEvent.InitiationTimestamp;
    result.connectedToSystemTimestamp = kinesisEvent.ConnectedToSystemTimestamp;
    result.initiationMethod = kinesisEvent.InitiationMethod;
    result.systemEndpoint = kinesisEvent.SystemEndpoint;
    result.customerEndpoint = kinesisEvent.CustomerEndpoint;
    result.disconnectTimestamp = kinesisEvent.DisconnectTimestamp;
    result.duration = millisToMinutesAndSeconds(new Date(kinesisEvent.DisconnectTimestamp) - new Date(kinesisEvent.InitiationTimestamp));
    result.answeredDuration = kinesisEvent.ConnectedToSystemTimestamp != null ? millisToMinutesAndSeconds(Math.abs(new Date(kinesisEvent.DisconnectTimestamp) - new Date(kinesisEvent.ConnectedToSystemTimestamp))) : null;

    result.recording = kinesisEvent.Recording;

    //neccessary for server logging
    if (kinesisEvent.Attributes.operatorId) result.operatorId = kinesisEvent.Attributes.operatorId;
    if (kinesisEvent.Attributes.writerId) result.writerId = kinesisEvent.Attributes.writerId;
    if (kinesisEvent.Attributes.orderId) result.orderId = kinesisEvent.Attributes.orderId;

    console.log("kinesisLog:", result);
    callback(null, result);
  }
  catch (ex) {
    callback(ex);
  }
};



function processingPromise(kinesisEvent) {
  return new Promise((resolve, reject) => {
    processKinesisEvent(kinesisEvent, function (error, result) {
      if (error) reject(error);
      resolve(result);
    });
  });
};

function fetchCollectedEvents() {
  if (!getItemParams)
    throw new Error('An error occurred. getItemParams is undefined')

  console.log("try to fetch data from DB", getItemParams);
  return getDynamoDbPromise(docClient, getItemParams);
};

function getDynamoDbPromise(documentClient, getItemParams) {
  return documentClient.get(getItemParams).promise();
};

function onFetchEventsSuccess(data) {
  if (!data.Item) {
    console.log("there is no existing items in Dynamo for this contactID");
    return {};
  }
  console.log("grabbed items from DynamoDB: ", data.Item.data.length);
  return data.Item;
}

function mergeDynamoKinesis(dynamoInfo, kinesisInfo) {
  var result = kinesisInfo;
  if (kinesisInfo.callStatus == callStatuses.AnsweredListenedConfirmed ){
    result.contactFlowId = dynamoInfo.contactFlowId;
    result.contactFlow = dynamoInfo.contactFlow;
    result.settedAttributes = dynamoInfo.settedAttributes;
    result.isContactFlowEnded = dynamoInfo.isContactFlowEnded;
    result.cloudWatchDuration = dynamoInfo.duration;
    result.details = getInfoFromLog(result);
  }
  else {
    result.details =  contactFlows[result.attributes.contactFlowId] + ". " + kinesisInfo.callStatus;
  }
  console.log("mergedLog:", result);

  return result;
};

function processDynamoItem(item) {
  if (!item.data) {
    return {};
  }
  var result = {};
  var notFlattenEvents = item.data.map(o => JSON.parse(o).logEvents)

  var logEvents = [].concat.apply([], notFlattenEvents).sort(function (a, b) { return a.timestamp - b.timestamp; });
  result.contactId = item.ContactId;
  result.duration = millisToMinutesAndSeconds(Math.abs(logEvents[logEvents.length - 1].timestamp - logEvents[0].timestamp));
  var logEventMessages = logEvents.map(o => JSON.parse(o.message.replace("\n", "")));

  result.contactFlowId = logEventMessages[0].ContactFlowId.split("/").slice(-1)[0];
  result.contactFlow = contactFlows[result.contactFlowId];
  result.settedAttributes = {};
  result.isContactFlowEnded = false;

  logEventMessages.forEach(function (event, index) {
    if (event.ContactFlowModuleType === "SetAttributes") {
      var temp = {};
      temp[event.Parameters.Key] = event.Parameters.Value
      result.settedAttributes[event.Parameters.Key] = event.Parameters.Value;
    }
    if (event.ContactFlowModuleType === "Disconnect") {
      result.isContactFlowEnded = true;
    }
  });

  result.isConfirmed = result.settedAttributes.isConfirmed || result.settedAttributes.isConfirmed === 'Yes';
  result.success = result.isConfirmed;
  console.log("dynamoDbItem:", result);
  return result;
};


function postDetailsToHost(dataToSend) {

  return new Promise((resolve, reject) => {
    var info = JSON.stringify(dataToSend);
    var destUrl = new url.parse(dataToSend.attributes.notifyUrl);
    try {
      var options = {
        hostname: destUrl.hostname,
        port: destUrl.port,
        path: destUrl.pathname,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': info.length
        }
      };
      console.log("sending request...", options);
      var req = http.request(options, function (res) {
        res.on('data', function (result) {
          try {
            result = JSON.parse(result.toString('utf-8'));
            if (result.success === true) {
              console.log("Successfully send info.");
            } else {
              console.log("Not success send.", result);
              reject(result);
            }
          }
          catch (ex) {
            console.log("exception in ondata handler: ", ex);
            reject(ex);
          }
        });
        res.on('end', function () {
          console.log('No more data in response.');
          resolve();
        });
      });

      req.on('error', function (e) {
        console.log('problem with request: ' + e.toString());
        reject(e);
      });

      // write data to request body
      console.log("write ", info)
      req.write(info);
      req.end();

    } catch (ex) {
      console.log("exception: ", ex);
      reject(ex);
    }
  });
}


function getInfoFromLog(processedLog) {
  var s1 = processedLog.attributes.isConfirmed === 'Yes' ? "Confirmed." : processedLog.settedAttributes.isConfirmed === 'No' ? "Not Confirmed." : "Confirmation is not recognized.";
  var s2 = !!processedLog.isContactFlowEnded ? "Listened to the end." : "Disconnected before the end.";
  var s3 = "Duration - " + processedLog.answeredDuration + ".";
  var s4 = processedLog.contactFlow + ".";
  var details = s4 + " " + s3 + " " + s2 + " " + s1;
  console.log("details: ", details);
  return details
};