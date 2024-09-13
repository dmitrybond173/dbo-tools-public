# SrvcLstNjs: Services List - node.js edition

Just front-end to list of Windiws services which allows to start/stop services.

# Recommended Setup

## Prerequisities 

To run this tool you need following:
* node.js (20+)
* NSSM (2.24+)

## Setup Instructions

1. run `npm pack`, it will build `srvclstnjs-nnnn.n.nnnnnn.tgz` (where n is digit reflecting version#)
2. extract to `C:\inetpub\wwwroot\SrvcLstNjs\` or other location you want to use for it
3. switch to location where you extracted it, run `npm install` to download all required packages
4. create `logs` sub-directory
5. check configuration parameters in `config/*.json` - if you are ok with it?
6. ensure NSSM.exe tool is available, run `setNssmSrvc.cmd`
7. start `SrvcLstNjs` service (you can use `restart.cmd`)
8. start browser try to open url like `http://localhost:1001` (or other port# - depending on what specified in `config/*.json`)

## Configuration

This tool designed to control only services which match service-name patterns defined in `config/*.json`
It supports 3 types of patterns:
* exact service name match
* wildcards - when you can use ? or * chars
* regexp - when pattern started and finished with "/" inside you can specify normal regular expression

# License

MIT

# Author

Dmitry Bond. 
* dima_ben@ukr.net
* dmitry.bond.real@gmail.com

