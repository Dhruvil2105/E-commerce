CREATE DATABASE ecommerce_identity;
CREATE USER identity_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_identity TO identity_user;

CREATE DATABASE ecommerce_product;
CREATE USER product_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_product TO product_user;

CREATE DATABASE ecommerce_order;
CREATE USER order_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_order TO order_user;

CREATE DATABASE ecommerce_payment;
CREATE USER payment_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_payment TO payment_user;

CREATE DATABASE ecommerce_inventory;
CREATE USER inventory_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_inventory TO inventory_user;

CREATE DATABASE ecommerce_notification;
CREATE USER notification_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_notification TO notification_user;