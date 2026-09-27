# Validation — TB-TMAR-HOST-CONTENT-AMC-001-R5

Overall: PASS

| Check | OK | Detail |
|---|---|---|
| sot_json_parse | True | True |
| manifest_json_parse | True | True |
| content_r4_checkpoint_master | True | master has R4 + stop |
| content_r4_checkpoint_protocol | True | protocol has R4 |
| semantic_contracts_master | True | master semantic |
| semantic_contracts_protocol | True | protocol semantic |
| semantic_contracts_standard | True | standard semantic |
| shallow_commands_and_queries_master | True | master CQ |
| shallow_commands_and_queries_protocol | True | protocol CQ |
| shallow_commands_and_queries_standard | True | standard CQ |
| one_file_folder_disallowed_standard | True | standard one-file ban |
| responsibility_not_relocation_master | True | master meaning |
| responsibility_not_relocation_protocol | True | protocol meaning |
| no_r3_as_latest_content | True | no R3-latest claim |
| next_host_not_started | True | stop next folder |
| sot_hostContentAmcR4 | True | sot R4 |
| sot_content_certified | True | Content in certifiedModules vicinity |
| protocol_no_fulfillment_as_current_next | True | stale fulfillment next removed |

Production builds: not required (docs-only).
