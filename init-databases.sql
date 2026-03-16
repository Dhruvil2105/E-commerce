-- ─────────────────────────────────────────────────────────────
-- Database per Service — enforced at infrastructure level
--
-- Each service gets:
--   1. Its own database     → complete data isolation
--   2. Its own user account → can ONLY connect to its own DB
--
-- This means even if a developer accidentally writes code
-- that tries to query another service's database,
-- PostgreSQL will reject the connection with an auth error.
-- The isolation is enforced by the database itself —
-- not just by coding convention.
--
-- This script runs automatically when PostgreSQL
-- container starts for the first time.
-- ─────────────────────────────────────────────────────────────

-- ── Identity Service ────────────────────────────────────────
CREATE DATABASE ecommerce_identity;
CREATE USER identity_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_identity TO identity_user;

-- ── Product Service ─────────────────────────────────────────
CREATE DATABASE ecommerce_product;
CREATE USER product_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_product TO product_user;

-- ── Order Service ───────────────────────────────────────────
CREATE DATABASE ecommerce_order;
CREATE USER order_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_order TO order_user;

-- ── Payment Service ─────────────────────────────────────────
CREATE DATABASE ecommerce_payment;
CREATE USER payment_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_payment TO payment_user;

-- ── Inventory Service ───────────────────────────────────────
CREATE DATABASE ecommerce_inventory;
CREATE USER inventory_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_inventory TO inventory_user;

-- ── Notification Service ────────────────────────────────────
CREATE DATABASE ecommerce_notification;
CREATE USER notification_user WITH PASSWORD 'secret';
GRANT ALL PRIVILEGES ON DATABASE ecommerce_notification TO notification_user;