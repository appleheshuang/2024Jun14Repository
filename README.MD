# Digital Test Automation

This repository contains env configuration and xunit tests to prepare data, execute APIs and check results. 

By default the pipelines for Digital tests. It can also be configured to execute Optimizer tests against the digital org.
Use TARGET_PRODUCT = digital (default), omr or all.
You will need to create files called smoketest-digital.json and ocesync-digital.json to run for the optimizer tests. These should be tenants with
* smoketest-digital.json -> digital + optimizer enabled
* ocesync-digital.json -> digital + optimizer + oce-personal enabled

In other respects the configuratin is the same as for [Optimizer](https://gitlab.ims.io/omr/omrtestautomation/-/blob/master/README.md "Optimizer readme")


Eva -- Use it as reference project, won't actually use it for testing
