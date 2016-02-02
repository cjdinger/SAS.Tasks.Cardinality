/* ------------------------------------------------------------------------------ */
/* count distinct values foreach char variable, generate proc freq statement */
%macro getcardinality(inputData, outputData, createReport);

%local cnt tot outlib outds;

%let outlib=%sysfunc(substr(&inputData.,1,%sysfunc(index(&inputData.,.))-1));
%let outds =%sysfunc(substr(&inputData.,1+%sysfunc(index(&inputData.,.))));

proc datasets library=&outlib. nolist;
  contents data=&outds. out=work._ds_variables(keep=name type format length) noprint;
run; quit;

data _null_;
  set &outlib..&outds. nobs=total;
  call symput('tot',strip(total));
  stop;
run;

data _null_;
  set work._ds_variables nobs=total;
  call symput('cnt',strip(total));
  stop;
run;
proc sql noprint;
  select trim(name) into :char1-:char&cnt. from work._ds_variables;
quit;

ods output nlevels=work.char_freqs;
ods noproctitle;
title "Summary of levels in &outlib..&outds.";
proc freq data=&outlib..&outds. nlevels;
  tables
  %do i=1 %to &cnt.;
    &&char&i.
  %end;
  / noprint;
run;
ods proctitle;

proc sql noprint;
  create table &outputdata. as
    select c.*, 
      ifc(c.type=1,'Numeric','Character') label='Type' as vartype ,
      r.nlevels, r.nlevels / &tot. as PCT_UNIQUE format=percent9.1
      from work._ds_variables c, work.char_freqs r
      where c.name=r.tablevar
    order by r.nlevels descending ;

  drop table work._ds_variables;
  drop table work.char_freqs;
quit;

%if (&createReport) %then %do;
  title "Cardinality of variables in &outlib..&outds.";
  title2 "Data contains &tot. rows";
  proc print data=&outputData. label noobs;
  label NLevels="Cardinality" pct_unique="Pct Unique";
  var name vartype length format nlevels pct_unique;
run;
%end;

%mend;