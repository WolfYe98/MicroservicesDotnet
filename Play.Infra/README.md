# docker-compose.yml
In VSCode, the docker extension will give us a intellisense for docker-compose.yml file.\

## Steps to define a docker-compose.yml file

### First Step: version
The first thing we have to do is to define the version of the compose file, because depending on the version, there are something includes or not.\
```yml
version: "3.8"
```

### Second Step: services
This block will define which services we want to have a container for.
```yml
services:
    mongo:
        image: mongo
        container_name: mongodb
        ports:
            - 27017:27017
        volumes:
            - mongodbdata:/data/db
```
Under the services, we define the first service that is mongo, is super important the tab before mongo, it defines that mongo is under services and it is a service.\
Here in this step we are indicating that the mongo service should use the image `mongo`, the container will have the name `mongodb`, the ports will be `27017:27017` and the volume will be mongodbdata:/data/db. The ports are a array of two values, that is why its have to be under the ```ports:``` section with a '-'. Same for the volume's value.\

As we can see, under the services sectiton we define our services and the options that will have (like the options in the `docker run` command).

### Third step: volumes
Now we have to define the volumes that we want to use. This section is another section apart of services. As we are telling in the section before that mongo will use `mongodbdata` as the volume, we have to define that volume here.
```yml
volumes:
    mongodbdata:
```
The `volumes` under the `services: mongo:` section tell the container that should mount a local directory called `mongodbdata` in the docker directory `/data/db`, and this `volumes:mongodbdata` section creates the directory `mongodbdata` in the local machine, this is mandatory if we want to use some volume.


## Run the docker compose
We can run it with the command
```
docker-compose up
```
If we don't want that the terminal only shows dockers information, we can run:
```
docker-compose up -d
```
just like `docker run`

**NOTE: the volumes created by docker-compose are brand new volumes, that means that if we had some volume created before with `docker run`, docker compose will not use those volumes, those volumes will only be available with `docker run`.**