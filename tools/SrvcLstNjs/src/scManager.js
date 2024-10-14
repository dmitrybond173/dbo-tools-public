/*
* Parser of SC command output.
* 
* date: July 31, 2024
* Author: Dmitry Bond. (dmitry.bond.real@gmail.com)
*/

import { CONFIG } from './config.js';
import * as chpr from 'node:child_process';
import wcmatch from 'wildcard-match';


const F_Srvc_Type = 0x0001;
const F_Srvc_State = 0x0002;
const F_Srvc_WinExitCode = 0x0004;
const F_Srvc_SvcExitCode = 0x0008;
const F_Srvc_ChkPoint = 0x0010;
const F_Srvc_WaitHint = 0x0020;

export const E_SrvcStatus_Stopped = 1;
export const E_SrvcStatus_StartPending = 2;
export const E_SrvcStatus_StopPending = 3;
export const E_SrvcStatus_Running = 4;
export const E_SrvcStatus_ContinuePending = 5;
export const E_SrvcStatus_PausePending = 6;
export const E_SrvcStatus_Paused = 7;


/** 
 * Holder of service information
*/
export class ServiceDescriptor {
    name;
    displayName;
    type; typeInfo;
    state; stateInfo;
    winExitCode;
    serviceExitCode;
    checkPoint;
    waitHint;
    #controllable = undefined;
    owner;

    flags = 0; 

    toString() {
        return `svc[${this.name}/${this.displayName}, t:${this.type}, st:${this.state}, win:${this.winExitCode}]`; 
    }

    setFlag(flag) {
        this.flags |= flag;
    }

    get stateAsString() {
        let result = "(unknown)";
        switch (parseInt(this.state))
        {
            case E_SrvcStatus_Stopped: result = "Stopped"; break;
            case E_SrvcStatus_StartPending: result =  "StartPending"; break;
            case E_SrvcStatus_StopPending: result = "StopPending"; break;
            case E_SrvcStatus_Running: result = "Running"; break;
            case E_SrvcStatus_ContinuePending: result = "ContinuePending"; break;
            case E_SrvcStatus_PausePending: result = "PausePending"; break;
            case E_SrvcStatus_Paused: result = "Paused"; break;
        }
        //console.log(` state = ${typeof this.state} => ${result}`);
        return result;
    }

    get isControllable() {
        if (this.#controllable != undefined)
            return this.#controllable;

        let value =  this.name.toLowerCase();
        let result = (CONFIG.contollableServices.length == 0);
        if (!result) {
            let z = CONFIG.contollableServices.find(it => {
                if (it.startsWith('/') && it.endsWith('/')) {
                    let expr = it.slice(1, it.length-1);
                    let rexp = new RegExp(expr, 'im');
                    result = (rexp.exec(value) != null);
                    //console.log(` ? rexp: ${result} <= [${expr}] vs. [${value}]`);
                } else if (it.includes("?") || it.includes("*")) {
                    result = wcmatch(it.toLowerCase())(value);
                    //console.log(` ? wc: ${result} <= [${it.toLowerCase()}] vs. [${value}]`);
                } else {
                    //console.log(` ? match: ${result} <= [${it.toLowerCase()}] vs. [${value}]`);
                    result = (it.toString().toLowerCase() === value)
                }
                return result;
            });
            result = (z != undefined)
        }
        this.#controllable = result;
        return result;
    }

    static Statuses() {
        return {
            Stopped: E_SrvcStatus_Stopped,
            StartPending: E_SrvcStatus_StartPending,
            StopPending: E_SrvcStatus_StopPending,
            Running: E_SrvcStatus_Running,
            ContinuePending: E_SrvcStatus_ContinuePending,
            PausePending: E_SrvcStatus_PausePending,
            Paused: E_SrvcStatus_Paused,            
        }
    }
}


/** 
 * Parser of SC command output
*/
export class ScOutputParser {
    #currentService;

    #rexpServiceName;
    #rexpServiceDisplayName;
    #rexpType;
    #rexpState;
    #rexpWinExitCode;
    #rexpServiceExitCode;
    #rexpCheckPoint;
    #rexpWaitHint;

    services;

    constructor() {
        this.#currentService = null;

        this.#rexpServiceName = /^SERVICE_NAME\:\s+(.*)$/m;
        this.#rexpServiceDisplayName = /^DISPLAY_NAME\:\s+(.*)$/m;
        this.#rexpType = /^\s+TYPE\s+\:\s+([0-9a-f]+)\s+(.*)$/mi;
        this.#rexpState = /^\s+STATE\s+\:\s+(\d+)\s+(.*)$/m;
        this.#rexpWinExitCode = /^\s+WIN32_EXIT_CODE\s+\:\s+(\d+)\s+(.*)$/m;
        this.#rexpServiceExitCode = /^\s+SERVICE_EXIT_CODE\s+\:\s+(\d+)\s+(.*)$/m;
        this.#rexpCheckPoint = /^\s+CHECKPOINT\s+\:\s+(\d+)\s+(.*)$/m;
        this.#rexpWaitHint = /^\s+WAIT_HINT\s+\:\s+(\d+)\s+(.*)$/m;

        this.services = [];
    }

    #ensureNewDescriptor() {
        if (this.#currentService == null)
        {
            this.#currentService = new ServiceDescriptor();
            this.#currentService.owner = this;
            //console.log("+++ new Service()");
            this.services.push(this.#currentService);
        }
    }

    feedServicesList(line) {
        //console.log("  ? check line: " + line);
    
        var result;
    
        result = this.#rexpServiceName.exec(line);
        if (result != null) {
            //console.log("  = match ServiceName");
            this.#currentService = null;
            this.#ensureNewDescriptor();
            this.#currentService.name = result[1].trimEnd();
            return ;
        }

        result = this.#rexpServiceDisplayName.exec(line);
        if (result != null) {
            //console.log(" +++ match DisplayName");
            this.#ensureNewDescriptor();
            this.#currentService.displayName = result[1].trimEnd();
            return ;
        }

        result = this.#rexpType.exec(line);
        if (result != null) {
            //console.log(" +++ match Type");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_Type);
                this.#currentService.type = result[1].trimEnd();
                this.#currentService.typeInfo = result[2].trimEnd();
            }
            return ;
        }

        result = this.#rexpState.exec(line);
        if (result != null) {
            //console.log(" +++ match State");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_State);
                this.#currentService.state = result[1].trimEnd();
                this.#currentService.stateInfo = result[2].trimEnd();
            }
            return ;
        }
    
        result = this.#rexpWinExitCode.exec(line);
        if (result != null) {
            //console.log(" +++ match WinExitCode");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_WinExitCode);
                this.#currentService.winExitCode = result[1].trimEnd();
            }
            return ;
        }

        result = this.#rexpServiceExitCode.exec(line);
        if (result != null) {
            //console.log(" +++ match ServiceExitCode");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_SvcExitCode);
                this.#currentService.serviceExitCode = result[1].trimEnd();
            }
            return ;
        }

        result = this.#rexpCheckPoint.exec(line);
        if (result != null) {
            //console.log(" +++ match CheckPoint");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_ChkPoint); 
                this.#currentService.serviceExitCode = result[1].trimEnd();
            }
            return ;
        }

        result = this.#rexpWaitHint.exec(line);
        if (result != null) {
            //console.log(" +++ match WaitHint");
            if (this.#currentService != null) {
                this.#currentService.setFlag(F_Srvc_WaitHint);
                this.#currentService.waitHint = result[1].trimEnd();
            }
            return ;
        }
    }

}


/** 
 * Service manager
*/
export class ScManager {
    #parser;

    loadedAt;
    services;

    constructor() {
        this.services = [];
        this.#parser = new ScOutputParser();
    }

    get servicesList() {
        return this.loadServicesList();
    }

    startService(srvcName) {
        return new Promise((resolve, reject) => {
            console.warn(`-> startService( ${srvcName} )...`);

            chpr.exec(`net start "${srvcName}"`, { windowsHide: true }, (error, stdout, stderr) => {
                if (error) {
                    console.error(`startService->exec error: ${error}`);
                    reject();
                    return;
                }
                var txt = stdout;               
                console.log(`startService->ok: ${txt}`);
                resolve();
            });
        });
    }

    stopService(srvcName) {
        return new Promise((resolve, reject) => {
            console.warn(`<- stopService( ${srvcName} )...`);

            chpr.exec(`net stop "${srvcName}"`, { windowsHide: true }, (error, stdout, stderr) => {
                if (error) {
                    console.error(`stopService->exec error: ${error}`);
                    reject();
                    return;
                }
                var txt = stdout;               
                console.log(`stopService->ok: ${txt}`);
                resolve();
            });
        });
    }

    loadServicesList() {
        return new Promise((resolve, reject) => {
            console.warn("loading services list...");

            chpr.exec('sc query state=all', { windowsHide: true }, (error, stdout, stderr) => {
                if (error) {
                    console.error(`exec error: ${error}`);
                    reject();
                    return;
                }
                var txt = stdout;
                
                // TO-DO: need to work with text re-coding - from console CP to UTF8 !
                //const dec = new TextDecoder("windows-1252")

                //console.error("[dbg] SC output: " + txt.substring(0, 500));
                var scOutput = txt.split('\n');
                this.parseServicesList(scOutput);
                this.services = this.#parser.services;
                this.loadedAt = new Date();
                resolve();
            });
        });
    }

    parseServicesList(lines) {
        //console.log(`${lines.length} lines in SC output...`);
        //console.error(" - reset services list");
        this.#parser.services = [];
        //console.error(" - parse outout");
        lines.forEach(line => {
            //DBG: if (this.#parser.services.length > 5) return ;
            this.#parser.feedServicesList(line);
        });        
        console.warn(`${this.#parser.services.length} services loaded...`);
    }


}

//module.exports = { ScOutputParser };
//export default ScOutputParser;