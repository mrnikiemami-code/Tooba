--
-- PostgreSQL database dump
--

\restrict zcXsuM6qSVdH0gnfAefJOq9fTEQGbAPcs7HHBFSkgDJffLlXzVSON9yuSNPQfHI

-- Dumped from database version 17.10 (Debian 17.10-1.pgdg13+1)
-- Dumped by pg_dump version 17.10 (Debian 17.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: cart_shipping_drafts; Type: TABLE; Schema: order; Owner: -
--

CREATE TABLE "order".cart_shipping_drafts (
    cart_id uuid NOT NULL,
    guest_secret_hash character varying(128) NOT NULL,
    cart_version integer NOT NULL,
    recipient_name character varying(128) NOT NULL,
    contact_mobile character varying(32) NOT NULL,
    province_name character varying(64) NOT NULL,
    city_name character varying(64) NOT NULL,
    postal_address character varying(512) NOT NULL,
    postal_code character varying(16) NOT NULL,
    saved_address_id uuid,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    shipping_amount numeric(18,2) NOT NULL,
    minimum_delivery_date date NOT NULL,
    selected_delivery_date date,
    selected_delivery_time_window character varying(32),
    customer_note character varying(500),
    updated_at timestamp with time zone NOT NULL,
    first_name character varying(64) DEFAULT ''::character varying NOT NULL,
    last_name character varying(64) DEFAULT ''::character varying NOT NULL
);


--
-- Data for Name: cart_shipping_drafts; Type: TABLE DATA; Schema: order; Owner: -
--

COPY "order".cart_shipping_drafts (cart_id, guest_secret_hash, cart_version, recipient_name, contact_mobile, province_name, city_name, postal_address, postal_code, saved_address_id, shipping_method_code, shipping_method_label, shipping_amount, minimum_delivery_date, selected_delivery_date, selected_delivery_time_window, customer_note, updated_at, first_name, last_name) FROM stdin;
01a09aff-11f8-7000-a6ae-7dcccc2abbdf		16	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	post:express	پست — پیشتاز	200000.00	2026-09-18	2026-09-18	12-15	\N	2026-09-13 13:47:06.202402+00		
01a09b8b-a8bc-7000-9c15-253c13ac45a5		17	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-19	2026-09-19	9-12	\N	2026-09-14 13:08:14.98573+00	محمد	امامی
01a13622-4b0e-7000-8cb3-7de5adf0d77b		8	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	tipax:standard	تیپاکس — معمولی	180000.00	2026-10-20	2026-10-22	12-15	\N	2026-10-14 02:19:43.256268+00		
01a138fb-03e1-7000-b9c7-79e87a4a6d43		14	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	tipax:express	تیپاکس — پیشتاز	260000.00	2026-10-18	2026-10-19	18-21	\N	2026-10-14 05:57:45.856832+00		
01a09b1e-9ea7-7000-8916-032f3a972abb		9	محمد امامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	post:express	پست — پیشتاز	200000.00	2026-09-18	2026-09-18	9-12	\N	2026-09-13 14:28:21.670894+00	محمد	امامی
01a09b2c-8e54-7000-996a-209d27727e59		5	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 14:30:14.712654+00	محمد	امامی
01a09b2e-0e3c-7000-a433-8b0b6672f37d		5	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 14:31:53.144816+00	محمد	امامی
01a09b30-a54b-7000-94ad-e9326593aebc		5	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 14:34:42.844956+00	محمد	امامی
01a09b31-2b8a-7000-8726-3bf390cd1d3c		6	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 14:36:08.047149+00	محمد	امامی
01a09b32-787c-7000-91b9-0be7d96bf8c4		6	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 14:37:15.919716+00	محمد	امامی
01a09b33-819c-7000-a3b6-2479c63c653f		6	محمد امامی	09111111111	تهران	تهران	تهران خیابان آزمایش ۱۲	1234567890	\N	post:express	پست — پیشتاز	200000.00	2026-09-16	2026-09-16	9-12	\N	2026-09-13 16:13:08.990091+00	محمد	امامی
01a12b64-aec6-7000-9812-eb8abf12f51e		20	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	post:express	پست — پیشتاز	200000.00	2026-10-17	2026-10-17	12-15	\N	2026-10-12 05:31:11.338525+00		
01a132ff-6cde-7000-8b82-286e7c0f5b47		7	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	post:express	پست — پیشتاز	200000.00	2026-10-18	2026-10-20	12-15	\N	2026-10-13 02:14:13.068783+00		
01a135e9-a0ac-7000-b0a9-8d7f50bc5859		7	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	01a09a72-0fd0-7000-b93f-907016579c4a	post:express	پست — پیشتاز	200000.00	2026-10-18	2026-10-20	12-15	\N	2026-10-13 15:43:41.071484+00		
\.


--
-- Name: cart_shipping_drafts pk_cart_shipping_drafts; Type: CONSTRAINT; Schema: order; Owner: -
--

ALTER TABLE ONLY "order".cart_shipping_drafts
    ADD CONSTRAINT pk_cart_shipping_drafts PRIMARY KEY (cart_id);


--
-- PostgreSQL database dump complete
--

\unrestrict zcXsuM6qSVdH0gnfAefJOq9fTEQGbAPcs7HHBFSkgDJffLlXzVSON9yuSNPQfHI

