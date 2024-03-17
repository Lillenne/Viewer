### Description
This project is a simple Blazor photo sharing app with upload, download, and limited sharing features.

![Demo image](/demo.png)

### Current status
This project is on hold (perhaps indefinitely). It has been mostly a practice project to familiarize myself with web technologies, Blazor, ORMs, patterns for distributed and asynchronous messaging, app hosting, and security. I have moved to a self-hosted [Immich](https://immich.app/) instance for my immediate sharing and backup needs.

### Goals of this project
* Share photos of my burgeoning immediate family with my extended family
* Make the tool that my wife thinks she wants for getting said photos to her phone / PC without paying for cloud internet storage
* Gain practical experience with project development, ui, databases, security, and web dev


### Roadmap
* ~~Image viewer UI~~ &rarr; Completed 5/29/23
* ~~Jwt Authentication~~ &rarr; Completed 6/2/23
* ~~User credential database~~ &rarr; Completed 6/2/23
  * ~~Registration return urls~~ &rarr; Completed 7/3/23
* Image server implementation 1 (app local file system)
  * ~~Serve~~ &rarr; Completed 7/5/23
  * ~~Download~~ Completed 2023
  * ~~Upload~~ Completed 2023
* User groups and photo view/share permissions
  * Roles (Authorization)
* Configure hosting & First release
* Telemetry
  * Track site access patterns
  * Track what images are downloaded
* ~~Image server implementation 2 (likely self-hosted MinIO)~~ Minio prototype completed 2023
    * Serve
    * Download
    * Upload
