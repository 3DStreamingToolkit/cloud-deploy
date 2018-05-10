FROM node:8

RUN mkdir src

WORKDIR ../src

COPY . ../src

USER node

RUN npm install

RUN npm start