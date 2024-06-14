var axios = require('axios');
var sleep = require('thread-sleep');
var FormData = require('form-data');
var myArgs = process.argv.slice(2);
 
var projectID = 4586;
var triggerPipeLine = 'https://gitlab.ims.io/api/v4/projects/'+ projectID +'/';
var privateToken = 'rHHt85q1WBLiEoMVyEZL';
var triggerToken = '0b2e569f12a948a1d71c44675e43b7';
var env = myArgs.length >= 1 ? myArgs[0] : 'master';
var searchedJob = myArgs.length >= 2 ? myArgs[1] : 'NONE';
var envConfig = myArgs.length >= 3 ? myArgs[2] : 'DEFAULT';

// //DEBUG
// var env = 'master';
// var searchedJob = 'OData_Test';
// var envConfig = 'BLKLOAD01-scratchorg.json';

async function POSTrun(){

    var POSTdata = new FormData();
    POSTdata.append('ref', env);
    POSTdata.append('token', triggerToken);
    
    var POSTconfig = {
      method: 'post',
      url: triggerPipeLine + 'trigger/pipeline',
      headers: {
         ...POSTdata.getHeaders()
      },
      data : POSTdata
    };

    var POSTid = await new Promise ((resolve, reject) => axios(POSTconfig)
        .then(function (response) {
            resolve(response.data.id);
            reject('ERROR');
        })
    )
    return POSTid;
}

async function GETpipelinestatus(postResponseId){

    var GETconfig = {
      method: 'get',
      url: triggerPipeLine + "/pipelines/" + postResponseId,
      headers: { 
        'PRIVATE-TOKEN': privateToken
      }
    };

    var GETstatus = await new Promise ((resolve, reject) => axios(GETconfig)
        .then(function (response) {
            resolve(response.data.status);
        })
    )
    return GETstatus;
}

async function GETjobs(pipeLineID){
    //I get the jobs
    var JOBSdata = new FormData();

    var JOBSconfig = {
      method: 'get',
      url: triggerPipeLine + '/pipelines/' + pipeLineID + '/jobs',
      headers: { 
        'PRIVATE-TOKEN': privateToken, 
        ...JOBSdata.getHeaders()
      },
      data : JOBSdata
    };    

    var jobs = await new Promise((resolve, reject) => axios(JOBSconfig)
      .then(function(response){
        resolve(response['data']);
      })
    )

    return jobs;
}

async function RUNjob(jobId){
  var RUNdata = new FormData();
  var RUNconfig = {
    method: 'post',
    url: triggerPipeLine + '/jobs/'+ jobId +'/play',
    headers: { 
      'PRIVATE-TOKEN': privateToken, 
      ...RUNdata.getHeaders()
    },
    data : RUNdata
  };          

  var jobStatus = await new Promise((resolve, reject) => axios(RUNconfig)
    .then(function (response) {
      // console.log(response['status']);
      resolve(response['status']);
      reject('ERROR');
    })
  ) 

  // return jobStatus;
}

async function GETjobStatus(jobId){
  var JOBdata = new FormData();

  var JOBconfig = {
    method: 'get',
    url: triggerPipeLine + '/jobs/' + jobId,
    headers: { 
      'PRIVATE-TOKEN': privateToken, 
      ...JOBdata.getHeaders()
    },
    data : JOBdata
  };   

  var getJobStatus = await new Promise((resolve, reject) => axios(JOBconfig)
   .then(function (response) {
     resolve(response['data']);
     reject('ERROR');
     // console.log(response);
   })
  )

  return getJobStatus['status'];
}

async function PUTEnvironmentVar(envConfig){
  var PUTenvdata = new FormData();
  PUTenvdata.append('value', envConfig);
  
  var PUTenvconfig = {
    method: 'put',
    url: triggerPipeLine +  'variables/ENVIRONMENT_CONFIG',
    headers: { 
      'PRIVATE-TOKEN': privateToken, 
      ...PUTenvdata.getHeaders()
    },
    data : PUTenvdata
  };
  
  await new Promise ((resolve, reject) => axios(PUTenvconfig)
    .then(function (response) {
        resolve(response);
        reject('ERROR');
    })
  )  
}

/********** MAIN FUNCTION**********************/
async function Main() {  

    //Update Environment Variable
    await new Promise((resolve, reject) => PUTEnvironmentVar(envConfig)
      .then(function (response){
        resolve(response);
        reject('ERROR');
      })
    )

    //Run the PIPELINE
    var pipeLineID = await new Promise ((resolve, reject) => POSTrun()
        .then(function (response) {
            resolve(response);
            reject('ERROR running the pipeline');
        })
    )

    // console.log(pipeLineID);
   
    //ONLY RUN JOB Check if JOB Name was Provided
    if (searchedJob != 'NONE') {

        /**GET the JOB LIST*******************************************************/
        var jobs = await new Promise((resolve, reject) => GETjobs(pipeLineID)
        .then(function (response) {
            resolve(response);
        })
        )
        
        /**GET the specific JOBID and JOBStatus**********************************/
        var jobId;
        var jobRunStatus;
        // console.log(jobs); 
        jobs.forEach(element => { 
        if (element['name'] == searchedJob) {
            // console.log(element['status']);
            jobRunStatus = element['status'];
            jobId = element['id'];
        }
        });
    
        //WAIT for manual status
        while (jobRunStatus != 'manual') {
            sleep(10*1000);
            jobRunStatus = await new Promise((resolve, reject) => GETjobStatus(jobId)
            .then(function (response){
                resolve(response);
                reject('ERROR getting jobstatus: ' + jobId);
                // console.log('IN');
            })
            )
        }    

        //Run the JOB if manual
        if (jobRunStatus == 'manual') {
            await new Promise((resolve, reject) => RUNjob(jobId)
            .then(function (response) {
                resolve(response);  
                reject('ERROR running the JOB: ' + jobId);
            })
            )     
        }

        //  console.log(getJobStatus['status']);
    
        /**LOOP and Wait till the JOB COMPLETES**********************************************/
        var ctr = 0;
        var sleepTime = 10 * 1000; //10 secs
        var maxCtr = 360; //an hour wait
        var waitJobStatus = 'pending';

        while (waitJobStatus != 'failed' && waitJobStatus != 'success' && ctr <= 360) {
            sleep(sleepTime);
            waitJobStatus = await new Promise((resolve, reject) => GETjobStatus(jobId)
            .then(function (response){
                resolve(response);
                reject('ERROR getting jobstatus: ' + jobId);
                ctr = ctr + 1;
                // console.log('IN');
            })
            )
        }          

        /**Update Environment Variable Back to DEFAULT****************************************/
        await new Promise((resolve, reject) => PUTEnvironmentVar('DEFAULT')
          .then(function (response){
            resolve(response);
            reject('ERROR');
          })
        )

        var url = 'https://gitlab.ims.io/omr/omrtestautomation/-/jobs/' + jobId;
        console.log(waitJobStatus + "|" + url);

    } else {

        /**JUST WAIT... *******************************************************************************/
        console.log('**NO JOBname was passed thus PIPELINE will be waited**');
        var ctr = 0;
        var sleepTime = 10 * 1000; //10 secs
        var maxCtr = 360; //an hour wait
        var waitPipelineStatus = 'pending';

        while (waitPipelineStatus != 'failed' && waitPipelineStatus != 'success' && ctr <= maxCtr) {
            sleep(sleepTime);
            waitPipelineStatus = await new Promise((resolve, reject) => GETpipelinestatus(pipeLineID)
            .then(function (response){
                resolve(response);
                reject('ERROR getting pipeline status: ' + pipeLineID);
                ctr = ctr + 1;
                // console.log('IN');
            })
            )
        }  

        /**Update Environment Variable Back to DEFAULT**************************************************/
        await new Promise((resolve, reject) => PUTEnvironmentVar('DEFAULT')
          .then(function (response){
            resolve(response);
            reject('ERROR');
          })
        )        

        var url = 'https://gitlab.ims.io/omr/omrtestautomation/pipelines/' + pipeLineID;
        console.log(waitPipelineStatus + "|" + url);

    }
  
}

Main();
