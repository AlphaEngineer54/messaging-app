﻿CREATE DATABASE IF NOT EXISTS auth_db;

USE auth_db;

CREATE TABLE User (
    Id INT NOT NULL AUTO_INCREMENT,
    Email VARCHAR(255),
    Password TEXT NOT NULL,
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
