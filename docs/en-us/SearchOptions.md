### Search Options

- If no search option is specified, an AND standard search is performed (the same as specifying “/and /text /m2”).
- The search options affect only the immediately following search word.
- Search options are not case sensitive.

#### Conjunction Options

Name|Description
-|-
/and|AND search (default)
/or|OR search
/not|NOT search

#### Property Options

Name|Description|Supplement
-|-|-
/text|Text (default)|String. File name.
/date|Date and Time|DateTime. Date of last update.
/size|Size|Numerals. File size.

#### Match Options

Name|Description|Supplement
-|-|-
/m0, /exact|ExactMatch| "Same as enclosing with double quotes." 
/m1, /word|WordMatch| Since we identify words by kind of letters, accuracy is not very good.
/m2|Standard (default)|
/re|RegularExpression| [.NET Regular Expression](https://docs.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference)
/ire|RegularExpression / IgnoreCase|Perform regular expression search without case sensitivity.
/since|DateTime / Since|After specified date
/until|DateTime / Until|Before specified date
/lt|Comparison operation / less than|Comparison operations work properly with sizes, dates, etc.
/le|Comparison operation / less than equal	
/eq|Comparison operation / equal	
/ne|Comparison operation / not equal	
/ge|Comparison operation / grater than equal	
/gt|Comparison operation / grater than

In case of Standard search or WordMatch search, it becomes ambiguous search as follows.

* (Japanese) Half-width and full-width characters are not distinguished.
* (Japanese) Hiragana and Katakana are not distinguished.
* Alphanumeric characters are not case-sensitive.
* Numbers do not distinguish when they start with “0”. (e.g.: file15” and “file0015” are not distinguished)
* UNICODE Normalization (NFKC).

### DateTime format

Normally, it is specified in the form "2019-04-01". Because [.NET date and time parsing](https://docs.microsoft.com/dotnet/standard/base-types/parsing-datetime) is used, you can specify other formats.

Note: The date designation "2019-04-01" is interpreted as "2019-04-01 00:00:00", so "/until 2019-04-01" does not include April 1.

You can also specify relative date and time in the format "-12day", "-6month" and "-1year". "/since -5day" means "within the past 5 days".

### Numeric format

Supports integers.

It also supports k, K, g, G, m, and M unit notations. Lowercase letters are multiplied by 1000, and uppercase letters are multiplied by 1024.

<br>
<br>

## Details

The previous options are aliased and simplified. Detailed definitions are given here.

### Search Unit

The basic unit of search is the following set

    Conjunction Option (/c.) | Property Option (/p.) | Match Option (/m.) | Keyword

A string that does not begin with a "/" is a keyword, and this set is determined when it appears. Options not previously specified will use default values, and duplicate category options will be overwritten.

### Options

Options are categorized by prefix character.

#### Conjunction Options (/c.)

Name|Description
-|-
/c.and|AND search (default)
/c.or|OR search
/c.not|NOT search

#### Property Options (/p.)

Name|Description|Type
-|-|-
/p.text|Text (default)|String
/p.date|Date and Time|DateTime
/p.size|Size|Integer

#### Match Options (/m.)

Name|Description|Type
-|-|-
/m.exact|ExactMatch|String
/m.word|WordMatch|String
/m.fuzzy|FuzzyMatch (default)|String
/m.re|RegularExpression|String
/m.ire|RegularExpression / IgnoreCase|String
/m.lt|Comparison operation / less than|Type of property option
/m.le|Comparison operation / less than equal|Type of property option
/m.eq|Comparison operation / equal|Type of property option
/m.ne|Comparison operation / not equal|Type of property option
/m.ge|Comparison operation / grater than equal|Type of property option
/m.gt|Comparison operation / grater than|Type of property option


The property and conforming options determine the type of value used for comparison, which is then converted to that type before the conformance check is performed.

The following example performs a date/time comparison as a DateTime type.

    /p.date /m.lt 2019-01-15

The following example compares the date and time as a string.

    /p.date /m.fuzzy 2019

### Alias

Options have aliases defined for ease of use, and in some cases are expanded into multiple options.

Alias|Decode
-|-
/and|/c.and
/or|/c.or
/not|/c.not
/text|/p.text
/re|/m.re
/ire|/m.ire
/m0|/m.exact
/exact|/m.exact
/m1|/m.word
/word|/m.word
/m2|/m.fuzzy
/fuzzy|/m.fuzzy
/lt|/m.lt
/le|/m.le
/eq|/m.eq
/ne|/m.ne
/ge|/m.ge
/gt|/m.gt
/date|/p.date
/since|/p.date /m.ge
/until|/p.date /m.le
/size|/p.size

<br>
<br>

## Examples

AND-Standard search of "ABC", "DEF"

    ABC DEF

RegularExpression search of "^ABC$"

    /re ^ABC$

ExactMatch search for "ABC DEF"

    "ABC DEF"

"DEF" NOT-WordMatch search for "ABC" Standard search results

    ABC /not /word DEF

Search for files updated in April 2019

    /since 2019-04-01 /until 2019-05-01

Search for files less than 10 MB in size

    /size /lt 10M
    
