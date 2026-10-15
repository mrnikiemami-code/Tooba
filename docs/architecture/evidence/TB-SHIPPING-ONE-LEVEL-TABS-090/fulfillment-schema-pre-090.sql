--
-- PostgreSQL database dump
--

\restrict GPaxpL39ZAsUi16yTNeKWWrdnIsGLwSp8IGM8GMhB7yCfntjgiT9yCCivl4cxoD

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

--
-- Name: fulfillment; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA fulfillment;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL
);


--
-- Name: consolidated_package_members; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.consolidated_package_members (
    consolidated_package_member_id uuid NOT NULL,
    consolidated_package_id uuid NOT NULL,
    shipment_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    joined_at timestamp with time zone NOT NULL,
    released_at timestamp with time zone
);


--
-- Name: consolidated_packages; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.consolidated_packages (
    consolidated_package_id uuid NOT NULL,
    package_number character varying(32) NOT NULL,
    checkout_id uuid NOT NULL,
    status character varying(32) NOT NULL,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    tracking_reference character varying(128),
    note character varying(512),
    created_by uuid,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    dispatched_at timestamp with time zone,
    delivered_at timestamp with time zone,
    cancelled_at timestamp with time zone
);


--
-- Name: fulfillments; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.fulfillments (
    fulfillment_id uuid NOT NULL,
    seller_order_id uuid NOT NULL,
    checkout_id uuid NOT NULL,
    seller_party_id uuid NOT NULL,
    placed_by_user_id uuid NOT NULL,
    status character varying(32) NOT NULL,
    recipient_name character varying(256) NOT NULL,
    contact_mobile character varying(32) NOT NULL,
    province_name character varying(128) NOT NULL,
    city_name character varying(128) NOT NULL,
    postal_address character varying(512) NOT NULL,
    postal_code character varying(32) NOT NULL,
    shipping_method_code character varying(64) NOT NULL,
    shipping_method_label character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL
);


--
-- Name: items; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.items (
    fulfillment_item_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    order_line_id uuid NOT NULL,
    quantity_ordered numeric(18,6) NOT NULL,
    quantity_shipped numeric(18,6) NOT NULL,
    reservation_id uuid,
    reservation_consumed boolean NOT NULL,
    quantity_packed numeric(18,6) DEFAULT 0 NOT NULL,
    quantity_processing numeric(18,6) DEFAULT 0 NOT NULL
);


--
-- Name: outbox_messages; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.outbox_messages (
    id uuid NOT NULL,
    occurred_at timestamp with time zone NOT NULL,
    event_type character varying(256) NOT NULL,
    payload text NOT NULL,
    correlation_id character varying(128),
    version integer NOT NULL,
    tenant_id character varying(128),
    deployment_id character varying(128) NOT NULL,
    edition character varying(32) NOT NULL,
    processed_at timestamp with time zone,
    attempt_count integer NOT NULL,
    next_attempt_at timestamp with time zone,
    dead_lettered_at timestamp with time zone,
    last_error character varying(256),
    locked_until timestamp with time zone
);


--
-- Name: payment_inbox; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.payment_inbox (
    event_id uuid NOT NULL,
    payment_id uuid NOT NULL,
    processed_at timestamp with time zone NOT NULL
);


--
-- Name: shipment_items; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipment_items (
    shipment_item_id uuid NOT NULL,
    shipment_id uuid NOT NULL,
    order_line_id uuid NOT NULL,
    quantity numeric(18,6) NOT NULL
);


--
-- Name: shipments; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipments (
    shipment_id uuid NOT NULL,
    fulfillment_id uuid NOT NULL,
    status character varying(32) NOT NULL,
    carrier_display_name character varying(128) NOT NULL,
    tracking_reference character varying(128),
    dispatched_at timestamp with time zone,
    delivered_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    shipping_method_code character varying(64) DEFAULT ''::character varying NOT NULL,
    shipping_method_label character varying(128) DEFAULT ''::character varying NOT NULL,
    provider_metadata_json text,
    provider_metadata_version integer DEFAULT 0 NOT NULL,
    previous_tracking_reference character varying(128)
);


--
-- Name: shipping_service_option_translations; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipping_service_option_translations (
    translation_id uuid NOT NULL,
    shipping_service_option_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL
);


--
-- Name: shipping_service_options; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipping_service_options (
    shipping_service_option_id uuid NOT NULL,
    shipping_service_id uuid NOT NULL,
    code character varying(64) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL
);


--
-- Name: shipping_service_translations; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipping_service_translations (
    translation_id uuid NOT NULL,
    shipping_service_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(128) NOT NULL,
    description character varying(512)
);


--
-- Name: shipping_services; Type: TABLE; Schema: fulfillment; Owner: -
--

CREATE TABLE fulfillment.shipping_services (
    shipping_service_id uuid NOT NULL,
    code character varying(64) NOT NULL,
    provider_kind character varying(64) NOT NULL,
    icon_key character varying(32) NOT NULL,
    color_key character varying(32) NOT NULL,
    sort_order integer NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL
);


--
-- Data for Name: __ef_migrations_history; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.__ef_migrations_history (migration_id, product_version) FROM stdin;
20260827010000_InitialFulfillment	8.0.11
20260907090000_AddFulfillmentItemQuantityPacked	8.0.11
20260908010000_ShipmentPreviousTracking	8.0.11
20260908120000_AddFulfillmentItemQuantityProcessing	8.0.0
20260909130500_DecimalFulfillmentQuantity	8.0.11
20260907121000_AddShipmentProviderMetadata	8.0.11
20260910120000_AddConsolidatedPackages	8.0.11
20260910170000_AddShippingServiceCatalog	9.0.0
\.


--
-- Data for Name: consolidated_package_members; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.consolidated_package_members (consolidated_package_member_id, consolidated_package_id, shipment_id, seller_party_id, fulfillment_id, joined_at, released_at) FROM stdin;
01a12f79-6ef2-7000-9a81-7a9ff7894683	01a12f79-6ef1-7000-8578-dd7cc841195d	01a12f79-1ad4-7000-9174-3237c0d645bf	01a030d1-40cb-7000-8abe-6d31739956c5	01a12e98-77aa-7000-9147-6b2d69ca0ee7	2026-10-12 09:37:29.841136+00	\N
01a12f79-6ef2-7000-9d73-e0edfbb5412d	01a12f79-6ef1-7000-8578-dd7cc841195d	01a12f79-33ed-7000-b7ad-e562dcf67be8	01a03826-97c5-7000-ad15-c9d141b1f32e	01a12e98-77d1-7000-aa6c-78f53c983947	2026-10-12 09:37:29.841136+00	\N
01a1382a-a66e-7000-a659-9c8d6c9a0761	01a1382a-a66d-7000-a58d-8adf554df475	01a13829-9fec-7000-862a-cf795b29ee42	01a030d1-40cb-7000-8abe-6d31739956c5	01a135f0-d50b-7000-92ff-28f5f3ecc65d	2026-10-14 02:08:01.64512+00	\N
01a1382a-a66e-7000-bb22-0c7d6a73ef25	01a1382a-a66d-7000-a58d-8adf554df475	01a1382a-4eef-7000-ae52-3ef9b5bbef04	01a03826-97c5-7000-ad15-c9d141b1f32e	01a135f0-d533-7000-9fe8-f1e75da9c348	2026-10-14 02:08:01.64512+00	\N
\.


--
-- Data for Name: consolidated_packages; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.consolidated_packages (consolidated_package_id, package_number, checkout_id, status, shipping_method_code, shipping_method_label, tracking_reference, note, created_by, created_at, updated_at, dispatched_at, delivered_at, cancelled_at) FROM stdin;
01a12f79-6ef1-7000-8578-dd7cc841195d	MP-01A12F796EF1	01a12e97-ee9e-7000-826c-a9eca536b223	Delivered	post	پست	125444	\N	01a036c2-970e-7000-8eb7-94bf5cc2d8db	2026-10-12 09:37:29.841136+00	2026-10-13 15:47:56.656863+00	2026-10-13 01:54:03.837141+00	2026-10-13 15:47:56.656863+00	\N
01a1382a-a66d-7000-a58d-8adf554df475	MP-01A1382AA66D	01a135ef-0c71-7000-a610-471ac77caf2d	Created	snapp_courier	اسنپ / پیک آنلاین	1255544	\N	01a036c2-970e-7000-8eb7-94bf5cc2d8db	2026-10-14 02:08:01.64512+00	2026-10-14 02:08:01.64512+00	\N	\N	\N
\.


--
-- Data for Name: fulfillments; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.fulfillments (fulfillment_id, seller_order_id, checkout_id, seller_party_id, placed_by_user_id, status, recipient_name, contact_mobile, province_name, city_name, postal_address, postal_code, shipping_method_code, shipping_method_label, created_at, updated_at) FROM stdin;
01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eebc-7000-bc5e-833d05c71b12	01a12e97-ee9e-7000-826c-a9eca536b223	01a030d1-40cb-7000-8abe-6d31739956c5	01a0996c-b8d9-7000-9854-80b3f59e8d7c	Delivered	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	post:express	پست — پیشتاز	2026-10-12 05:31:46.465609+00	2026-10-13 15:47:56.587471+00
01a12e98-77d1-7000-aa6c-78f53c983947	01a12e97-eed9-7000-9535-d01f854819c9	01a12e97-ee9e-7000-826c-a9eca536b223	01a03826-97c5-7000-ad15-c9d141b1f32e	01a0996c-b8d9-7000-9854-80b3f59e8d7c	Delivered	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	post:express	پست — پیشتاز	2026-10-12 05:31:46.465609+00	2026-10-13 15:47:56.639409+00
01a135f0-d50b-7000-92ff-28f5f3ecc65d	01a135ef-0c8d-7000-9f60-0f1a48b6f579	01a135ef-0c71-7000-a610-471ac77caf2d	01a030d1-40cb-7000-8abe-6d31739956c5	01a0996c-b8d9-7000-9854-80b3f59e8d7c	Packed	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	post:express	پست — پیشتاز	2026-10-13 15:45:38.037207+00	2026-10-14 02:07:18.771209+00
01a135f0-d533-7000-9fe8-f1e75da9c348	01a135ef-0c9c-7000-b0fc-cae4fc9a86c2	01a135ef-0c71-7000-a610-471ac77caf2d	01a03826-97c5-7000-ad15-c9d141b1f32e	01a0996c-b8d9-7000-9854-80b3f59e8d7c	Packed	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	post:express	پست — پیشتاز	2026-10-13 15:45:38.037207+00	2026-10-14 02:07:46.43311+00
01a13891-baf9-7000-bc80-97d47b18e76b	01a13835-5b51-7000-a983-84cba5b6abbd	01a13835-5b41-7000-a115-0b3a43a52375	01a030d1-40cb-7000-8abe-6d31739956c5	01a0996c-b8d9-7000-9854-80b3f59e8d7c	ReadyToFulfill	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	tipax:standard	تیپاکس — معمولی	2026-10-14 04:00:37.092283+00	2026-10-14 04:00:37.092283+00
01a138fd-d2d2-7000-a707-f0d0b99275ff	01a138fc-fb3b-7000-8e99-f9cc4103710c	01a138fc-fb2d-7000-8b85-0f904327bedc	01a030d1-40cb-7000-8abe-6d31739956c5	01a0996c-b8d9-7000-9854-80b3f59e8d7c	Processing	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	tipax:express	تیپاکس — پیشتاز	2026-10-14 05:58:41.099695+00	2026-10-14 05:59:59.774937+00
01a13891-baf3-7000-b757-a55c6049956e	01a13835-5b4d-7000-8ea2-fcce36e43088	01a13835-5b41-7000-a115-0b3a43a52375	01a0ffff-0001-7000-8000-000000000001	01a0996c-b8d9-7000-9854-80b3f59e8d7c	ReadyToFulfill	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	tipax:standard	تیپاکس — معمولی	2026-10-14 04:00:37.092283+00	2026-10-14 04:02:39.418859+00
01a13891-baec-7000-9878-8220eaee3940	01a13835-5b49-7000-820e-13979a27e4c4	01a13835-5b41-7000-a115-0b3a43a52375	01a03826-97c5-7000-ad15-c9d141b1f32e	01a0996c-b8d9-7000-9854-80b3f59e8d7c	ReadyToFulfill	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	tipax:standard	تیپاکس — معمولی	2026-10-14 04:00:37.092283+00	2026-10-14 05:12:00.359286+00
01a138fd-d2d8-7000-b4e0-067706a032a1	01a138fc-fb41-7000-a7b7-01f135818a8f	01a138fc-fb2d-7000-8b85-0f904327bedc	01a0ffff-0001-7000-8000-000000000001	01a0996c-b8d9-7000-9854-80b3f59e8d7c	ReadyToFulfill	محمد لمامی	09111111111	تهران	تهران	تهران تستی	1988989	tipax:express	تیپاکس — پیشتاز	2026-10-14 05:58:41.099695+00	2026-10-14 05:58:41.099695+00
\.


--
-- Data for Name: items; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.items (fulfillment_item_id, fulfillment_id, order_line_id, quantity_ordered, quantity_shipped, reservation_id, reservation_consumed, quantity_packed, quantity_processing) FROM stdin;
01a138fd-d2d2-7000-9a42-ee8a8ec4fe3c	01a138fd-d2d2-7000-a707-f0d0b99275ff	01a138fc-fb41-7000-bd28-06c012a0d3f6	5.000000	0.000000	01a138fc-fb5a-7000-94fb-d81802ce6200	f	0.000000	5.000000
01a12e98-77ad-7000-8a59-02cf7a089372	01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eed1-7000-b039-34c3aaca97c2	1.000000	1.000000	01a12e97-ef81-7000-a1ef-89d69275fd03	t	1.000000	1.000000
01a12e98-77ad-7000-8e03-2e01d172f603	01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eed5-7000-af42-30aa3d9e17e0	2.000000	2.000000	01a12e97-ef84-7000-9312-003a14e8bb47	t	2.000000	2.000000
01a12e98-77ad-7000-9f08-1f63083d99ab	01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eecd-7000-ba48-61ac1a09943d	6.000000	6.000000	01a12e97-ef7b-7000-90cf-f2b797bdad0c	t	6.000000	6.000000
01a12e98-77ad-7000-a86f-46946f16a7cd	01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eec9-7000-9165-c31c1eece4d1	4.000000	4.000000	01a12e97-ef54-7000-a6ad-427b6e7ce95d	t	4.000000	4.000000
01a12e98-77ad-7000-bc5d-96980e9c0515	01a12e98-77aa-7000-9147-6b2d69ca0ee7	01a12e97-eed8-7000-a2da-0a676d1395b8	1.000000	1.000000	01a12e97-ef87-7000-882c-94ffb7e8c675	t	1.000000	1.000000
01a12e98-77d2-7000-9ead-087db14bd4d0	01a12e98-77d1-7000-aa6c-78f53c983947	01a12e97-eedc-7000-bf18-cb684bdc521e	5.000000	5.000000	01a12e97-ef78-7000-9854-7b1a4affaad3	t	5.000000	5.000000
01a135f0-d50d-7000-b823-ac12506e6e50	01a135f0-d50b-7000-92ff-28f5f3ecc65d	01a135ef-0c95-7000-b721-0e52ae7585cc	1.000000	0.000000	01a135ef-0d05-7000-84d0-e61d2b1e631d	f	1.000000	1.000000
01a135f0-d50e-7000-b676-ddbeb7679f78	01a135f0-d50b-7000-92ff-28f5f3ecc65d	01a135ef-0c9c-7000-b48a-3fe2181fa391	1.000000	0.000000	01a135ef-0d3e-7000-aee0-48afe81f5f93	f	1.000000	1.000000
01a135f0-d533-7000-9579-76ce50f63a32	01a135f0-d533-7000-9fe8-f1e75da9c348	01a135ef-0ca1-7000-8e77-3a52ea7898ec	1.000000	0.000000	01a135ef-0d37-7000-aa64-39a19f001125	f	1.000000	1.000000
01a13891-baf9-7000-8b91-39ca333c532e	01a13891-baf9-7000-bc80-97d47b18e76b	01a13835-5b57-7000-a2a0-55e4bc771722	1.000000	0.000000	01a13835-5b6c-7000-8cdf-45d474a3cea7	f	0.000000	0.000000
01a13891-baf3-7000-9864-71ae4820c604	01a13891-baf3-7000-b757-a55c6049956e	01a13835-5b51-7000-823e-f98e0e6613f0	1.000000	0.000000	01a13835-5b68-7000-8e00-6ae81f215d98	f	0.000000	0.000000
01a13891-baec-7000-be8a-413a9457eb44	01a13891-baec-7000-9878-8220eaee3940	01a13835-5b4d-7000-a520-31bfd7c07bd5	1.000000	0.000000	01a13835-5b64-7000-badb-31d62a44c347	f	0.000000	0.000000
01a138fd-d2d8-7000-9555-a16ab26c7fdd	01a138fd-d2d8-7000-b4e0-067706a032a1	01a138fc-fb4b-7000-8ea9-4a2acbca1456	4.000000	0.000000	01a138fc-fb5f-7000-b3af-39990eef81d2	f	0.000000	0.000000
\.


--
-- Data for Name: outbox_messages; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.outbox_messages (id, occurred_at, event_type, payload, correlation_id, version, tenant_id, deployment_id, edition, processed_at, attempt_count, next_attempt_at, dead_lettered_at, last_error, locked_until) FROM stdin;
01a13891-baf9-7000-a504-23529afd077d	2026-10-14 04:00:37.113807+00	fulfillment.created.v1	{"fulfillmentId":"01a13891-baf9-7000-bc80-97d47b18e76b","sellerOrderId":"01a13835-5b51-7000-a983-84cba5b6abbd","checkoutId":"01a13835-5b41-7000-a115-0b3a43a52375"}	deb0d3f9c50e494c8b9b6d3d9f9cd426	1	store-alpha	dev-singlestore	SingleStore	2026-10-14 04:00:38.660752+00	1	\N	\N	\N	\N
01a12f98-fa63-7000-90bb-aef8dc5318f0	2026-10-12 10:11:57.155594+00	shipment.dispatched.v1	{"fulfillmentId":"01a12e98-77aa-7000-9147-6b2d69ca0ee7","shipmentId":"01a12f79-1ad4-7000-9174-3237c0d645bf","sellerOrderId":"01a12e97-eebc-7000-bc5e-833d05c71b12"}	25fed562edb04276b1327a9f627ecb4f	1	store-alpha	dev-singlestore	SingleStore	2026-10-12 10:11:57.822831+00	1	\N	\N	\N	\N
01a132f7-8174-7000-905d-28031bff5d8c	2026-10-13 01:54:03.76467+00	shipment.dispatched.v1	{"fulfillmentId":"01a12e98-77d1-7000-aa6c-78f53c983947","shipmentId":"01a12f79-33ed-7000-b7ad-e562dcf67be8","sellerOrderId":"01a12e97-eed9-7000-9535-d01f854819c9"}	e4bd52054b984ac0bf35bde95eea147e	1	store-alpha	dev-singlestore	SingleStore	2026-10-13 01:54:05.527777+00	1	\N	\N	\N	\N
01a135f0-d50e-7000-9d1f-f09453e36223	2026-10-13 15:45:38.062384+00	fulfillment.created.v1	{"fulfillmentId":"01a135f0-d50b-7000-92ff-28f5f3ecc65d","sellerOrderId":"01a135ef-0c8d-7000-9f60-0f1a48b6f579","checkoutId":"01a135ef-0c71-7000-a610-471ac77caf2d"}	ee85e14135dc4a5cabb49a854a24061e	1	store-alpha	dev-singlestore	SingleStore	2026-10-13 15:45:39.946945+00	1	\N	\N	\N	\N
01a135f0-d533-7000-9747-f28236bba545	2026-10-13 15:45:38.099576+00	fulfillment.created.v1	{"fulfillmentId":"01a135f0-d533-7000-9fe8-f1e75da9c348","sellerOrderId":"01a135ef-0c9c-7000-b0fc-cae4fc9a86c2","checkoutId":"01a135ef-0c71-7000-a610-471ac77caf2d"}	ee85e14135dc4a5cabb49a854a24061e	1	store-alpha	dev-singlestore	SingleStore	2026-10-13 15:45:39.950731+00	1	\N	\N	\N	\N
01a12e98-77ae-7000-8694-73db6b6d45e3	2026-10-12 05:31:46.478103+00	fulfillment.created.v1	{"fulfillmentId":"01a12e98-77aa-7000-9147-6b2d69ca0ee7","sellerOrderId":"01a12e97-eebc-7000-bc5e-833d05c71b12","checkoutId":"01a12e97-ee9e-7000-826c-a9eca536b223"}	03f680d569374aeaa17088717b338bc6	1	store-alpha	dev-singlestore	SingleStore	2026-10-12 05:31:48.509116+00	1	\N	\N	\N	\N
01a12e98-77d2-7000-b5ce-40f09a3c4fbb	2026-10-12 05:31:46.514028+00	fulfillment.created.v1	{"fulfillmentId":"01a12e98-77d1-7000-aa6c-78f53c983947","sellerOrderId":"01a12e97-eed9-7000-9535-d01f854819c9","checkoutId":"01a12e97-ee9e-7000-826c-a9eca536b223"}	03f680d569374aeaa17088717b338bc6	1	store-alpha	dev-singlestore	SingleStore	2026-10-12 05:31:48.514187+00	1	\N	\N	\N	\N
01a138fd-d2d2-7000-8165-99cdd77f3216	2026-10-14 05:58:41.10621+00	fulfillment.created.v1	{"fulfillmentId":"01a138fd-d2d2-7000-a707-f0d0b99275ff","sellerOrderId":"01a138fc-fb3b-7000-8e99-f9cc4103710c","checkoutId":"01a138fc-fb2d-7000-8b85-0f904327bedc"}	383f58fb135d4d8586c7c0620cf50c43	1	store-alpha	dev-singlestore	SingleStore	2026-10-14 05:58:41.245209+00	1	\N	\N	\N	\N
01a13891-baec-7000-b5c9-9829735fe478	2026-10-14 04:00:37.100574+00	fulfillment.created.v1	{"fulfillmentId":"01a13891-baec-7000-9878-8220eaee3940","sellerOrderId":"01a13835-5b49-7000-820e-13979a27e4c4","checkoutId":"01a13835-5b41-7000-a115-0b3a43a52375"}	deb0d3f9c50e494c8b9b6d3d9f9cd426	1	store-alpha	dev-singlestore	SingleStore	2026-10-14 04:00:38.652784+00	1	\N	\N	\N	\N
01a13891-baf3-7000-9814-006a65ec152d	2026-10-14 04:00:37.107495+00	fulfillment.created.v1	{"fulfillmentId":"01a13891-baf3-7000-b757-a55c6049956e","sellerOrderId":"01a13835-5b4d-7000-8ea2-fcce36e43088","checkoutId":"01a13835-5b41-7000-a115-0b3a43a52375"}	deb0d3f9c50e494c8b9b6d3d9f9cd426	1	store-alpha	dev-singlestore	SingleStore	2026-10-14 04:00:38.656737+00	1	\N	\N	\N	\N
01a138fd-d2d8-7000-9fd6-55f88191af86	2026-10-14 05:58:41.112607+00	fulfillment.created.v1	{"fulfillmentId":"01a138fd-d2d8-7000-b4e0-067706a032a1","sellerOrderId":"01a138fc-fb41-7000-a7b7-01f135818a8f","checkoutId":"01a138fc-fb2d-7000-8b85-0f904327bedc"}	383f58fb135d4d8586c7c0620cf50c43	1	store-alpha	dev-singlestore	SingleStore	2026-10-14 05:58:41.250088+00	1	\N	\N	\N	\N
\.


--
-- Data for Name: payment_inbox; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.payment_inbox (event_id, payment_id, processed_at) FROM stdin;
01a12e98-7252-7000-b137-48bf0e1f9a29	01a12e98-5459-7000-bcd0-7a7d2a9e7165	2026-10-12 05:31:46.465609+00
01a135f0-cccc-7000-9289-54881f74bc97	01a135ef-0e56-7000-982e-d29a0bde9998	2026-10-13 15:45:38.037207+00
01a13891-bac2-7000-b580-5c58cf909771	01a13835-5b98-7000-a380-7b46ebd3e7be	2026-10-14 04:00:38.694023+00
01a138fd-d2b1-7000-8972-eca2fafb6eaf	01a138fc-fb8e-7000-95dc-cbc4bd3e74fc	2026-10-14 05:58:41.280095+00
\.


--
-- Data for Name: shipment_items; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipment_items (shipment_item_id, shipment_id, order_line_id, quantity) FROM stdin;
01a12f79-1ad5-7000-8794-cf46da771017	01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e97-eed5-7000-af42-30aa3d9e17e0	2.000000
01a12f79-1ad5-7000-9e4b-9c1bde4faa43	01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e97-eed1-7000-b039-34c3aaca97c2	1.000000
01a12f79-1ad5-7000-a3a1-cfaa2d67756a	01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e97-eecd-7000-ba48-61ac1a09943d	6.000000
01a12f79-1ad5-7000-a3ed-b55c413a570a	01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e97-eed8-7000-a2da-0a676d1395b8	1.000000
01a12f79-1ad5-7000-b27c-a700706d8651	01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e97-eec9-7000-9165-c31c1eece4d1	4.000000
01a12f79-33ed-7000-8d87-f6d511fbfb6e	01a12f79-33ed-7000-b7ad-e562dcf67be8	01a12e97-eedc-7000-bf18-cb684bdc521e	5.000000
01a13829-9fee-7000-86cc-93d8fcd3617d	01a13829-9fec-7000-862a-cf795b29ee42	01a135ef-0c95-7000-b721-0e52ae7585cc	1.000000
01a13829-9fee-7000-96f1-40187f4804c1	01a13829-9fec-7000-862a-cf795b29ee42	01a135ef-0c9c-7000-b48a-3fe2181fa391	1.000000
01a1382a-4ef0-7000-87fe-88bb36eac06c	01a1382a-4eef-7000-ae52-3ef9b5bbef04	01a135ef-0ca1-7000-8e77-3a52ea7898ec	1.000000
01a138fe-ac1b-7000-a9f2-40a2cf255760	01a138fe-ac1b-7000-9c97-a0b9c8e7f621	01a138fc-fb41-7000-bd28-06c012a0d3f6	5.000000
\.


--
-- Data for Name: shipments; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipments (shipment_id, fulfillment_id, status, carrier_display_name, tracking_reference, dispatched_at, delivered_at, created_at, shipping_method_code, shipping_method_label, provider_metadata_json, provider_metadata_version, previous_tracking_reference) FROM stdin;
01a138fe-ac1b-7000-9c97-a0b9c8e7f621	01a138fd-d2d2-7000-a707-f0d0b99275ff	Cancelled	پست	TRK-01A138FE	\N	\N	2026-10-14 05:59:36.731106+00	post	پست	{"ServiceType":"معمولی","PackageCount":1,"WeightKg":null,"Dimensions":null,"DeclaredValue":null,"RecipientName":"محمد لمامی","RecipientPhone":"09111111111","PostalCode":"1988989","DestinationAddress":"تهران، تهران، تهران تستی، 1988989","Notes":null}	1	\N
01a12f79-1ad4-7000-9174-3237c0d645bf	01a12e98-77aa-7000-9147-6b2d69ca0ee7	Delivered	پست	125444-01A12F79	2026-10-12 10:11:57.155071+00	2026-10-13 15:47:56.587471+00	2026-10-12 09:37:08.308444+00	post	پست	{"ServiceType":"پیشتاز","PackageCount":1,"WeightKg":null,"Dimensions":null,"DeclaredValue":null,"RecipientName":"محمد لمامی","RecipientPhone":"09111111111","PostalCode":"1988989","DestinationAddress":"تهران، تهران، تهران تستی، 1988989","Notes":null}	1	\N
01a12f79-33ed-7000-b7ad-e562dcf67be8	01a12e98-77d1-7000-aa6c-78f53c983947	Delivered	پست	125444-01A12F7933ED7000B7ADE562DCF67BE8	2026-10-13 01:54:03.764208+00	2026-10-13 15:47:56.639409+00	2026-10-12 09:37:14.733128+00	post	پست	{"ServiceType":"پیشتاز","PackageCount":1,"WeightKg":null,"Dimensions":null,"DeclaredValue":null,"RecipientName":"محمد لمامی","RecipientPhone":"09111111111","PostalCode":"1988989","DestinationAddress":"تهران، تهران، تهران تستی، 1988989","Notes":null}	1	\N
01a13829-9fec-7000-862a-cf795b29ee42	01a135f0-d50b-7000-92ff-28f5f3ecc65d	Created	اسنپ / پیک آنلاین	TRK-01A13829	\N	\N	2026-10-14 02:06:54.444809+00	snapp_courier	اسنپ / پیک آنلاین	{"PickupAddress":"تهران، تهران، تهران تستی، 1988989","DestinationAddress":"تهران، تهران، تهران تستی، 1988989","PickupContactName":"محمد لمامی","PickupContactPhone":"09111111111","RecipientName":"محمد لمامی","RecipientPhone":"09111111111","PickupWindow":null,"PackageDescription":null,"DriverNote":null,"ExternalReference":null}	1	\N
01a1382a-4eef-7000-ae52-3ef9b5bbef04	01a135f0-d533-7000-9fe8-f1e75da9c348	Created	اسنپ / پیک آنلاین	45458	\N	\N	2026-10-14 02:07:39.247998+00	snapp_courier	اسنپ / پیک آنلاین	{"PickupAddress":"تهران، تهران، تهران تستی، 1988989","DestinationAddress":"تهران، تهران، تهران تستی، 1988989","PickupContactName":"محمد لمامی","PickupContactPhone":"09111111111","RecipientName":"محمد لمامی","RecipientPhone":"09111111111","PickupWindow":null,"PackageDescription":null,"DriverNote":null,"ExternalReference":null}	1	\N
\.


--
-- Data for Name: shipping_service_option_translations; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipping_service_option_translations (translation_id, shipping_service_option_id, language_id, name) FROM stdin;
0f7efb50-76c7-4e0d-926b-1722fce005b8	af195675-5cf0-42ef-971c-12e9909bad51	01a06a53-4196-7000-9b98-fb3706490005	Express
644ff4f4-82c7-43a0-b775-cac54a7f3785	af195675-5cf0-42ef-971c-12e9909bad51	01a06a53-4139-7000-bb9b-3078a0d2e5c4	پیشتاز
90bfe637-61cc-4d5f-8d99-32094bfb13d8	ed76ebaf-1236-4776-827f-e1c25054c8b6	01a06a53-4196-7000-9b98-fb3706490005	Standard
afdef66f-f5c3-4fdc-b73e-2c9a0b295a47	ed76ebaf-1236-4776-827f-e1c25054c8b6	01a06a53-4139-7000-bb9b-3078a0d2e5c4	معمولی
01a138f1-4247-7000-8769-40f349c8a492	638c583e-4bce-4298-bdec-438b680af05e	01a06a53-4139-7000-bb9b-3078a0d2e5c4	پیشتاز
01a138f1-4247-7000-9bcc-cbb16f64b4c8	a871b290-c499-410d-b15d-e0659fc779ee	01a06a53-4139-7000-bb9b-3078a0d2e5c4	معمولی
01a138f1-4247-7000-a90e-7c1b0ef37b72	a871b290-c499-410d-b15d-e0659fc779ee	01a06a53-4196-7000-9b98-fb3706490005	Standard
01a138f1-4247-7000-bb3d-d692f9134c23	638c583e-4bce-4298-bdec-438b680af05e	01a06a53-4196-7000-9b98-fb3706490005	Express
\.


--
-- Data for Name: shipping_service_options; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipping_service_options (shipping_service_option_id, shipping_service_id, code, sort_order, is_active, created_at, updated_at) FROM stdin;
af195675-5cf0-42ef-971c-12e9909bad51	3dc3d04d-0d74-4475-96fd-85a168fd01da	express	10	t	2026-09-10 18:04:18.530339+00	2026-09-10 18:04:18.530339+00
ed76ebaf-1236-4776-827f-e1c25054c8b6	3dc3d04d-0d74-4475-96fd-85a168fd01da	standard	20	t	2026-09-10 18:04:18.530339+00	2026-09-10 18:04:18.530339+00
638c583e-4bce-4298-bdec-438b680af05e	6e9a4cdf-e40c-4271-aef9-bbe558c4d11e	express	10	f	2026-10-14 05:44:57.649633+00	2026-10-14 05:44:57.649633+00
a871b290-c499-410d-b15d-e0659fc779ee	6e9a4cdf-e40c-4271-aef9-bbe558c4d11e	standard	20	t	2026-10-14 05:44:57.649633+00	2026-10-14 05:44:57.649633+00
\.


--
-- Data for Name: shipping_service_translations; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipping_service_translations (translation_id, shipping_service_id, language_id, name, description) FROM stdin;
5e202fef-5136-477b-9fda-c57799ce17bb	0ed329bb-6563-4e5e-8d2d-55305f0cc02d	01a06a53-4196-7000-9b98-fb3706490005	Store courier	\N
5e9ab26b-f911-4f30-9f08-276668b5a44a	49ecbe67-0c00-4cdf-b1cc-969f39adf7de	01a06a53-4139-7000-bb9b-3078a0d2e5c4	اسنپ / پیک آنلاین	\N
69badae5-6cdf-41c9-87b3-9b113acd69e7	92c06402-8637-4d4d-a0ef-78ec0b312e2a	01a06a53-4196-7000-9b98-fb3706490005	In person	\N
6c16e72b-e585-45c4-9df5-c5fe9889841f	0ed329bb-6563-4e5e-8d2d-55305f0cc02d	01a06a53-4139-7000-bb9b-3078a0d2e5c4	پیک فروشگاه	\N
c8f402aa-94fd-45db-8b9b-193f8343b8c8	49ecbe67-0c00-4cdf-b1cc-969f39adf7de	01a06a53-4196-7000-9b98-fb3706490005	Snapp / Online courier	\N
eb926c52-f4ae-4515-b207-8339aed29bb3	92c06402-8637-4d4d-a0ef-78ec0b312e2a	01a06a53-4139-7000-bb9b-3078a0d2e5c4	تحویل حضوری	\N
97b48dd6-f446-4918-9e81-2a4b7876fe0c	3dc3d04d-0d74-4475-96fd-85a168fd01da	01a06a53-4196-7000-9b98-fb3706490005	Tipax	\N
b002010f-d3b3-4bb2-a929-913259bb5a24	3dc3d04d-0d74-4475-96fd-85a168fd01da	01a06a53-4139-7000-bb9b-3078a0d2e5c4	تیپاکس	\N
01a138f1-4239-7000-963e-e607f1993bfd	6e9a4cdf-e40c-4271-aef9-bbe558c4d11e	01a06a53-4196-7000-9b98-fb3706490005	Post	\N
01a138f1-4239-7000-a7b6-1d891bce3439	6e9a4cdf-e40c-4271-aef9-bbe558c4d11e	01a06a53-4139-7000-bb9b-3078a0d2e5c4	پست	\N
\.


--
-- Data for Name: shipping_services; Type: TABLE DATA; Schema: fulfillment; Owner: -
--

COPY fulfillment.shipping_services (shipping_service_id, code, provider_kind, icon_key, color_key, sort_order, is_active, created_at, updated_at) FROM stdin;
0ed329bb-6563-4e5e-8d2d-55305f0cc02d	store_courier	store_courier	bike	violet	40	t	2026-09-10 14:20:42.625576+00	2026-09-10 14:20:42.625576+00
92c06402-8637-4d4d-a0ef-78ec0b312e2a	in_person	in_person	store	rose	50	t	2026-09-10 14:20:42.625576+00	2026-09-10 14:20:42.625576+00
3dc3d04d-0d74-4475-96fd-85a168fd01da	tipax	tipax	tipax	amber	20	t	2026-09-10 14:20:42.625576+00	2026-09-10 18:04:18.530339+00
49ecbe67-0c00-4cdf-b1cc-969f39adf7de	snapp_courier	courier	courier	emerald	30	f	2026-09-10 14:20:42.625576+00	2026-10-14 05:44:17.384657+00
6e9a4cdf-e40c-4271-aef9-bbe558c4d11e	post	post	post	blue	10	t	2026-09-10 14:20:42.625576+00	2026-10-14 05:44:57.649633+00
\.


--
-- Name: __ef_migrations_history pk___ef_migrations_history; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.__ef_migrations_history
    ADD CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id);


--
-- Name: consolidated_package_members pk_consolidated_package_members; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.consolidated_package_members
    ADD CONSTRAINT pk_consolidated_package_members PRIMARY KEY (consolidated_package_member_id);


--
-- Name: consolidated_packages pk_consolidated_packages; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.consolidated_packages
    ADD CONSTRAINT pk_consolidated_packages PRIMARY KEY (consolidated_package_id);


--
-- Name: fulfillments pk_fulfillments; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.fulfillments
    ADD CONSTRAINT pk_fulfillments PRIMARY KEY (fulfillment_id);


--
-- Name: items pk_items; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.items
    ADD CONSTRAINT pk_items PRIMARY KEY (fulfillment_item_id);


--
-- Name: outbox_messages pk_outbox_messages; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.outbox_messages
    ADD CONSTRAINT pk_outbox_messages PRIMARY KEY (id);


--
-- Name: payment_inbox pk_payment_inbox; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.payment_inbox
    ADD CONSTRAINT pk_payment_inbox PRIMARY KEY (event_id);


--
-- Name: shipment_items pk_shipment_items; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipment_items
    ADD CONSTRAINT pk_shipment_items PRIMARY KEY (shipment_item_id);


--
-- Name: shipments pk_shipments; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipments
    ADD CONSTRAINT pk_shipments PRIMARY KEY (shipment_id);


--
-- Name: shipping_service_option_translations pk_shipping_service_option_translations; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipping_service_option_translations
    ADD CONSTRAINT pk_shipping_service_option_translations PRIMARY KEY (translation_id);


--
-- Name: shipping_service_options pk_shipping_service_options; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipping_service_options
    ADD CONSTRAINT pk_shipping_service_options PRIMARY KEY (shipping_service_option_id);


--
-- Name: shipping_service_translations pk_shipping_service_translations; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipping_service_translations
    ADD CONSTRAINT pk_shipping_service_translations PRIMARY KEY (translation_id);


--
-- Name: shipping_services pk_shipping_services; Type: CONSTRAINT; Schema: fulfillment; Owner: -
--

ALTER TABLE ONLY fulfillment.shipping_services
    ADD CONSTRAINT pk_shipping_services PRIMARY KEY (shipping_service_id);


--
-- Name: ix_consolidated_package_members_package_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_consolidated_package_members_package_id ON fulfillment.consolidated_package_members USING btree (consolidated_package_id);


--
-- Name: ix_consolidated_package_members_shipment_active; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_consolidated_package_members_shipment_active ON fulfillment.consolidated_package_members USING btree (shipment_id) WHERE (released_at IS NULL);


--
-- Name: ix_consolidated_packages_checkout_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_consolidated_packages_checkout_id ON fulfillment.consolidated_packages USING btree (checkout_id);


--
-- Name: ix_consolidated_packages_package_number; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_consolidated_packages_package_number ON fulfillment.consolidated_packages USING btree (package_number);


--
-- Name: ix_fulfillments_checkout_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_fulfillments_checkout_id ON fulfillment.fulfillments USING btree (checkout_id);


--
-- Name: ix_fulfillments_seller_order_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_fulfillments_seller_order_id ON fulfillment.fulfillments USING btree (seller_order_id);


--
-- Name: ix_fulfillments_seller_party_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_fulfillments_seller_party_id ON fulfillment.fulfillments USING btree (seller_party_id);


--
-- Name: ix_items_fulfillment_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_items_fulfillment_id ON fulfillment.items USING btree (fulfillment_id);


--
-- Name: ix_shipment_items_shipment_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_shipment_items_shipment_id ON fulfillment.shipment_items USING btree (shipment_id);


--
-- Name: ix_shipments_fulfillment_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_shipments_fulfillment_id ON fulfillment.shipments USING btree (fulfillment_id);


--
-- Name: ix_shipments_tracking_reference; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_shipments_tracking_reference ON fulfillment.shipments USING btree (tracking_reference) WHERE (tracking_reference IS NOT NULL);


--
-- Name: ix_shipping_service_option_translations_option_lang; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_shipping_service_option_translations_option_lang ON fulfillment.shipping_service_option_translations USING btree (shipping_service_option_id, language_id);


--
-- Name: ix_shipping_service_options_service_code; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_shipping_service_options_service_code ON fulfillment.shipping_service_options USING btree (shipping_service_id, code);


--
-- Name: ix_shipping_service_options_service_id; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE INDEX ix_shipping_service_options_service_id ON fulfillment.shipping_service_options USING btree (shipping_service_id);


--
-- Name: ix_shipping_service_translations_service_lang; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_shipping_service_translations_service_lang ON fulfillment.shipping_service_translations USING btree (shipping_service_id, language_id);


--
-- Name: ix_shipping_services_code; Type: INDEX; Schema: fulfillment; Owner: -
--

CREATE UNIQUE INDEX ix_shipping_services_code ON fulfillment.shipping_services USING btree (code);


--
-- PostgreSQL database dump complete
--

\unrestrict GPaxpL39ZAsUi16yTNeKWWrdnIsGLwSp8IGM8GMhB7yCfntjgiT9yCCivl4cxoD

