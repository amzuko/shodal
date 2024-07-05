using System.Collections;
using System.Collections.Specialized;

namespace shodal;

class Program
{

    private class Rule {
        public int id;
        public string a = "";
        public string b = "";
    }

    private static bool src_is_bank_a;

    private static char[] bank_a = new char[1024*1024];
    private static int a_count;
    private static char[] bank_b = new char[1024*1024];
    private static int b_count;

    private static List<Rule> rules = new List<Rule>();
    

    private static bool IsSpacer(char c) {
        return Char.IsWhiteSpace(c) || c == '(' || c == ')';
    }

    private static void WriteCharToDst(char c) {
        if (src_is_bank_a) {
            bank_b[b_count] = c;
            b_count++;
        } else {
            bank_a[a_count] = c;
            a_count++;
        }
    }

    private static void deviceWrite(char op, Dictionary<char, char[]> registers) {
        
        if (registers.ContainsKey('0')) {
            int acc = 0;
            if(!Int32.TryParse(registers['0'], out acc)) {
                Console.WriteLine($"Failed to parse r0: {new String(registers['0'])}");
                return;
            }
            int r1 = 0;
            if(!Int32.TryParse(registers['1'], out r1)) {
                Console.WriteLine($"Failed to parse r1: {new String(registers['1'])}");
                return;
            }
            // TODO: more ops, more registers
            switch(op) {
                case '+':
                    acc += r1;
                    break;
                case '-':
                    acc -= r1;
                    break;
                default:
                    Console.WriteLine($"Unknown op {op}");
                    break;
            }

            var res = acc.ToString();
            foreach (char c in res) {
                WriteCharToDst(c);
            }
            return;
        } else {
            //TODO: string
        }
    }

    private static void writeRegister(char rid, Dictionary<char, char[]> registers) {
        var reg = registers[rid];
        switch(rid) {
            case ':':
                var s = registers[rid][0];
                deviceWrite(s, registers);
                break;
            case '~':
                var input = Console.ReadLine();
                foreach (char c in input) {
                    WriteCharToDst(c);
                }
                break;
            default: 
                Span<char> target;
                if (src_is_bank_a) {
                    target = new Span<char>(bank_b, b_count, reg.Length);
                    b_count += reg.Length;
                } else {
                    target = new Span<char>(bank_a, a_count, reg.Length);
                    a_count += reg.Length;
                }

                reg.CopyTo(target);
                break;
        }
    }

    private static bool applyRule(Rule r, ReadOnlySpan<char> s) {
        // Console.WriteLine($"Trying {r.id}: {r.a}-->{r.b} to {s}");

        var registers = new Dictionary<char, char[]>();

        var a = r.a.AsSpan();
        var b = r.b.AsSpan();


        while(a.Length > 0 ) {
            if (s.Length == 0) {
                return false;
            }
            if(a[0] == '?') {
                var pcap = walk(s);
                if (pcap == 0) {
                    // Don't match empty strings
                    return false;
                }
                var regID = a[1];
                // Compare rule
                if (registers.ContainsKey(regID)) {
                    if (!registers[regID].AsSpan().SequenceEqual(s.Slice(0, pcap))) {
                        return false;
                    }
                } else {
                    registers.Add(regID, s.Slice(0, pcap).ToArray());
                }
                a = a.Slice(2);
                s = s.Slice(pcap);
            } else {
                if (a[0] != s[0]) {
                    return false;
                }
                s = s.Slice(1);
                a = a.Slice(1);
            }

        }

        // Console.WriteLine("matched!");
        // foreach(var k in registers.Keys) {
        //     Console.WriteLine($"'{k}':'{new String(registers[k])}'");
        // }
        if(b.Length == 0) {
            return false;
        }


        while(b.Length > 0) {
            if (b[0] == '?') {
                if(registers.ContainsKey(b[1])) {
                    writeRegister(b[1], registers);
                    b = b.Slice(1);
                }  else {
                    WriteCharToDst(b[0]);
                }
            } else {
                WriteCharToDst(b[0]);
            }
            b = b.Slice(1);
        }

        writeTail(s, r);
        return true;
    }

    private static void writeTail(ReadOnlySpan<char> s, Rule? r) {
    
        Span<char> target;
        if (src_is_bank_a) {
            target = new Span<char>(bank_b, b_count, s.Length);
        } else {
            target = new Span<char>(bank_a, a_count, s.Length);
        }

        s.CopyTo(target);

        if (src_is_bank_a) {
            a_count = 0;
            b_count += s.Length;
        } else {
            b_count = 0;
            a_count += s.Length;
        }

        if (r != null) {
            if (src_is_bank_a) {
                Console.WriteLine($"{r.id}: {bank_b.AsSpan().Slice(0, b_count)}");
            } else {
                Console.WriteLine($"{r.id}: {bank_a.AsSpan().Slice(0, a_count)}");
            }
        }

        src_is_bank_a = !src_is_bank_a;

    }
    
    private static ReadOnlySpan<char> ConsumeWhitespace(ReadOnlySpan<char> s) {
        while(s.Length >0 && Char.IsWhiteSpace(s[0])) {
            s = s.Slice(1);
        }
        return s;
    }

    private static int walk(ReadOnlySpan<char> start) {
        int depth = 0;
        var s = start;
        if (s[0] == '(') {
            while(s.Length > 0) {
                if (s[0] == '(') {
                    depth++;
                }
                if (s[0] == ')') {
                    depth--;
                }
                if (depth == 0) {
                    return start.Length - s.Length;
                }
                s  = s.Slice(1);
            }
        }

        while(s.Length > 0 && !IsSpacer(s[0])) {
            s = s.Slice(1);
        }

        return start.Length - s.Length;
    }

    private static  ReadOnlySpan<char> ParseFragment(ReadOnlySpan<char> s, ref string fragment) {
        s = ConsumeWhitespace(s);
        if (s.Length == 0) {
            fragment = "";
            return s;
        }
        char c = s[0];
        if(c == ')' || (c == '<' && s[1] == '>') || (c == '>' && s[1] == '<')) {
            fragment = "";
            return s;
        } else {
            var cap = walk(s);
            if(c == '(') {
                fragment = new string(s.Slice(1, cap -1));
                return s.Slice(cap + 1);
            } else {
                fragment = new string(s.Slice(0, cap));
                return s.Slice(cap);
            }
        }
    }

    private static Rule? findRule(ReadOnlySpan<char> s, int cap) {
        // if (s[0] == '(') {
        //     s = s.Slice(1);
        //     cap--;
        // }
        // foreach(Rule r in rules) {

        // }
        return null;
    } 


    private static string currentProgram() {
        var program ="";
        if (src_is_bank_a) {
            program = bank_a.ToArray().AsSpan().Slice(0, a_count).ToString();
        } else {
            program = bank_b.ToArray().AsSpan().Slice(0, b_count).ToString();
        }
        return program;
    }

    private static bool rewrite() {
        char current;
        char last = ' ';
        ReadOnlySpan<char> s;
        if (src_is_bank_a) {
            s = bank_a.AsSpan(0, a_count);
        } else {
            s = bank_b.AsSpan(0, b_count);
        }
        while(s.Length > 0) {
            current = s[0];
            if(current == '(' || IsSpacer(last)) {
                Rule r;
                // TODO: undefine
                if (current == '>' && s[1] == '<') {
                    //phase: define
                    s = s.Slice(2);
                    s = ConsumeWhitespace(s);
                    int cap = walk(s);
                    // writeTail(s, r);
                    return true;
                }

                if (current == '<' && s[1] == '>') {
                    r = new Rule();
                    s = s.Slice(2);

                    //phase: define
                    r.id = rules.Count;

                    s = ParseFragment(s, ref r.a);
                    s = ParseFragment(s, ref r.b);
                    rules.Add(r);
                    Console.WriteLine($"defined {r.id}: {r.a}-->{r.b}");
                    s = ConsumeWhitespace(s);
                    writeTail(s, null);
                    return true;
                }

                // phase lambda
                if (current == '?' && s[1] == '(') {
                    // var cap = walk(s.Slice(2));

                    r = new Rule();
                    r.id = -1;
                    s = s.Slice(2);
                    s = ParseFragment(s, ref r.a);
                    s = ParseFragment(s, ref r.b);
                    s = s.Slice(1); //trailing ')'
                    s = ConsumeWhitespace(s);

                    Console.WriteLine($"Lambda {r.a}-->{r.b}");
                    if(!applyRule(r, s)) {
                        Console.WriteLine($"Failed to apply lambda to '{s}'");
                        writeTail(s, r);        
                    }
                    return true;
                }

                // phase apply
                foreach(var rule in rules) {
                    if(applyRule(rule, s)) {
                        return true;
                    }
                }
            }
            last = current;
            WriteCharToDst(current);
            s = s.Slice(1);
        }
        return false;
    }

    static void Main(string[] args)
    {

        if (args.Length >2 || args.Length == 0 || (args.Length > 0 && args[0] == "-h")) {
            Console.WriteLine("Usage is ./shodal <source path> (max rewrites)");
        }
        
        var maxRetries = 100;
        if (args.Length > 1) {
            Int32.TryParse(args[1], out maxRetries);
        }
        var str = File.ReadAllText(args[0]);
        Console.WriteLine(args[0] + str);
        if (str.Length > bank_a.Length) {
            Console.WriteLine("Input is too long.");
            return;
        }
        str.CopyTo(bank_a);
        a_count = str.Length;
        src_is_bank_a = true;

        int rw = 0;

        while (rewrite() && rw < maxRetries) {
            rw++;
        }
        var program = currentProgram();
        Console.WriteLine($"Final program: '{program}' after {rw} rewrites");
    }
}
