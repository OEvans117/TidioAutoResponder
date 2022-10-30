Reverse engineering / C# project to automatically reply to Tidio messages with custom replies.

Settings.txt & TidioReplies:

* You can get the keys/ids by starting a websocket on tidio.
* Multithreaded=false means it will handle one conversation before replying to the other (realistic)

```
TidioReplies=TidioResponses.txt
AccessKey=
DeviceFingerprint=
ProjectPrivateKey=
ProjectPublicKey=
PersonalVisitorID=
OperatorID=
Multithreaded=false

9~>contains->(?:obtain|receive|get|download|(?:give|send) me|i need|can i have)(?: a | the | your | your |.)(?:free (?:trial|test|version|download|version)?|trial|test (?:free))->FAQ #4) How can I get a trial? : For trial, please leave a comment on the forum thread and we will get back to you.
7~>contains->(?:free|test) (?:trial|test|version|download|version)->FAQ #4) How can I get a trial? : For trial, please leave a comment on the forum thread and we will get back to you.
5~>contains->reviews->FAQ #5) Does your software work currently work? : /does-this-software-really-work/ | /reviews
