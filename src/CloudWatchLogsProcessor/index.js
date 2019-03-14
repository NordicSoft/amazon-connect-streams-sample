var AWS = require('aws-sdk'),
  docClient = new AWS.DynamoDB.DocumentClient({ region: 'us-east-1' }),
  zlib = require('zlib');

exports.handler = (event, context, callback) => {
  var payload = new Buffer(event.awslogs.data, 'base64');
  var newItemParams;
  gunzipPromise(payload)
    .then((result) => {
      var processedBinResult = processBinaryResult(result);
      newItemParams = processedBinResult.newItemParams;
      if (!newItemParams)
        throw new Error('An error occurred. newItemParams is undefined')

      return docClient.update(newItemParams).promise();
    })
    .then(() => {
      context.succeed("save data to Dynamo");
      context.done(null, "context done. saved to Dynamo");
    })
    .catch((err) => { console.log("error ", err, err.stack); context.fail(err); });
};

function gunzipPromise(payload) {
  return new Promise((resolve, reject) => {
    zlib.gunzip(payload, function (error, result) {
      if (error) reject(error);
      resolve(result);
    });
  })
};

function processBinaryResult(binResult) {
  var resultParsed = JSON.parse(binResult.toString('ascii'));
  var contactId = JSON.parse(resultParsed.logEvents[0].message).ContactId;
  getItemParams = {
    Key: { ContactId: contactId },
    TableName: "tempLogs"
  };
  var stringifyItem = JSON.stringify(resultParsed);
  newItemParams = {
    Key: { ContactId: contactId },
    TableName: "tempLogs",
    AttributeUpdates: {
      data: {
        Action: "ADD",
        Value: [stringifyItem]

      }
    },
    ReturnValues: "ALL_NEW"
  };

  return {
    getItemParams: getItemParams,
    newItemParams: newItemParams
  }
};
