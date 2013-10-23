/*
    HAL Javascript Wizard Framework
    ===============================

    HAL is a simple javascript framework to create a wizard something similar
    to the setup wizards used to install a program. Usually the major task is
    compleated at the end of the wizard and the  wizard is used to  ask a set
    of questions to the user.

    How to use HAL?
    Ans: Simply fill up the hal.config.xml and write any  custom user code in
    hal.user.js file. The system will run the wizard and update the  model as
    the user answers the wizard questions.

    How can one customize HAL?
    Ans: One can write code in the hal.user.js. The  following customizations
    are effortless. 
        
        1. Write custom actions. The developer can  write custom  actions for 
        usage along with the prebuilt actions like next previous etc.

        2. The developer can  customize the  look of  the widgets  by editing
        repective css classes.

        3. The developer can  also override  the implementations provided  by 
        hal widget builder and provide own implementation

 */


function _(text) {
    console.log(text);
}

function Prebuilt(halInstance) {
    
    var self = this;
    _(["Prebuilt constructor",self]);
    self.hal = halInstance;

    self.cmd = {};
    self.cmd.next = function () {
        if (self.hal.runtime.view_index + 1 < self.hal.cfg.view_config.order.length) {
            self.hal.runtime.view_index++;
            self.hal.gotoStep(hal.runtime.view_index);
        } else {
            _("next called but nowhere to go");
        }
    }
    self.cmd.prev = function () {
        if (self.hal.runtime.view_index > 0) {
            self.hal.runtime.view_index--
            self.hal.gotoStep(self.hal.runtime.view_index);
        }
        else {
            _("prev called but nowhere to return");
        }
    }
    self.cmd.exit_with_warning = function () {
        if (confirm("Are you sure that you want to exit?")) {
            window.polaris.system.quit(false);
        }
    }
}

/*
    Builder contains the commands to build the UI elements
 */



function Builder(halInstance) {
    
    var self = this;
    _(["Builder constructor",self]);
    self.maxId = 0;
    self.hal = halInstance;
    self.defs = {};
    self.execQueue = [];
    self.templates = {
        text:  '<p class="halui text" id="${id}">${text}</p>',
        input: '<table><tr><td style="width:100px"><span class="hal input">${text}</span></td><td style="width:100px"><input class="halui input" id="${id}"></input></td></tr></table>',
        radio: '<div class="halui radio" id="${id}"><p>${text}</p></div>',
        option: '<div><input class="halui input" id="${id}" type="radio" value="${value}" name="group1">${text}</input></br><div>',
        action: '<input class="hal action" type="button" value="${text}" onclick="javascript:hal.${cmd}()"></input>'
    };

    self.onBuildComplete = function () {
        while (self.execQueue.length > 0) {
            var fn = self.execQueue.pop();
            fn();
        }
    }
    
    self.updateModelFromStep = function () {
        _(["update model from step"])
        var ctls = $(".halui");
        _(["ctls",ctls])
        for (index in ctls) {
            var ctl = ctls[index];
            _(["ctl",ctl])
            var def = self.defs[ctl.id];
            _(["def", def])
            if (def != null) {
                if (def.model != null) {
                    var model = self.hal.cfg.client.model;
                    if (def._type == "input") {
                        model[def.model] = $(ctl).val();
                    }
                }
            }
        }
    }

    self.updateModel = function (def) {
        _(["In model", def]);
        if (def.model != null) {
            model = self.hal.cfg.client.model;
            var modelValue = null;
            try {
                modelValue = model[def.model];
            } catch (errMdl) {
                modelValue = null;
            }
            _(["model_value", modelValue]);
            if (modelValue == null) {
                model[def.model] = null;
            } else {
                if (def._type == "input") {
                    $("#" + def.id).val(modelValue);
                }
            }
        }

    }

    self.createWidget = function (def, parent, indent) {
        _(["Create widget", def, container]);
        // Make sure UI element has an id.
        if (def.id == null) {
            self.maxId++;
            def.id = "control_" + self.maxId.toString();
            self.defs[def.id] = def;
        }

        if (parent != null) {
            def.parent = parent;
        }

        if (indent == null) {
            indent = 0;
        }

        var tmpl = self.templates[def._type];
        var tempContainer = null;
        var output = null;

        var items = null;
        if (def.items != null) {
            items = def.items;
        }
        if (def.content != null) {
            if (def.content.items != null){
                items = def.content.items;
            }
        }

        var postExecute = null;

        if (items!= null){
            tempContainer = $("<div/>");
            for (key in items) {
                childItem = items[key];
                childWidget = self.createWidget(childItem, def, indent+1);
                childWidget.appendTo(tempContainer);
                _(["child item", childItem]);
                if (def._type == "option") {
                    postExecute = function () {

                        $("#parent_" + def.id).hide();
                        // bind to click 
                        for (key2 in parent.items) {
                            sibling = parent.items[key2];
                            $("#" + sibling.id).click(function () { 
                                _(["clicked", def, sibling]);
                                if ($("#" + def.id)[0].checked == true) {
                                    $("#parent_" + def.id).fadeIn();
                                } else {
                                    $("#parent_" + def.id).fadeOut();
                                }
                            });
                        }
                    };
                    self.execQueue.push(postExecute);
                    _(["set postExecute",def])
                }
            }
        }

        var fn_model = function () { self.updateModel(def); }
        self.execQueue.push(fn_model);

        output = $.tmpl(tmpl, def);
        if (tempContainer != null) {
            _(["temp container", tempContainer]);            
            tempContainer.attr("id", "parent_" + def.id);
            try {
                indentStr = (indent * 20).toString() + "px";
                tempContainer.css("padding-left", indentStr);
                tempContainer.css("padding-top", "5px");
            } catch (cssErr) { alert(cssErr);}
            tempContainer.appendTo(output);
        }

        return output;
    }
}


var hal = {};

function Hal(cfgInstance) {
    _("Hal constructor")
    var self = this;
    self.cfg = cfgInstance;


    self.init = function () {
        _(["Hal init", self]);

        self.prebuilt = new Prebuilt(self);
        self.builder = new Builder(self);
    };

    self.processConfig = function () {
        _("process config");
        self.cfg.view_config.order = eval(self.cfg.view_config.order);
        for (key in self.cfg.action_config.sets) {
            self.cfg.action_config.sets[key] = eval(self.cfg.action_config.sets[key]['#text']);
        }
        self.runtime = {};
        self.runtime.view_index = 0;
    };

    self.run = function () {
        _("run");
        self.processConfig();
        self.runtime.view_index = 0;
        self.gotoStep(self.runtime.view_index);
    };


    self.gotoStep = function (stepIndex) {
        _("goto");
        currentStep = self.cfg.steps[self.cfg.view_config.order[stepIndex]];
        document.getElementById(self.cfg.view_config.elements.header).innerText = currentStep.header;
        document.getElementById(self.cfg.view_config.elements.body).innerText = currentStep.body;
        self.buildUI(currentStep);
        self.buildNavigation(currentStep.actions);
    };

    self.buildNavigation = function (actionSetName) {
        _(["Build Navigation", actionSetName]);
        try {
            actionSet = hal.cfg.action_config.sets[actionSetName]
            nav = $("#" + hal.cfg.view_config.elements.nav);
            nav.html('');
            for (index in actionSet) {
                actionName = actionSet[index];
                action = hal.cfg.action_config.actions[actionName];
                if (action.text == null) {
                    action.text = action['#text'];
                }
                action._type = "action";
                self.builder.createWidget(action).appendTo(nav);
            }
        } catch (ex) { alert(ex.message); }
    };

    self.buildContent = function (content, container) {
        _(["Build Content", content,container]);
        for (index in content.items) {
            def = content.items[index];
            if (def != null) {
                if (def.text == null) {
                    def.text = def['#text'];
                }               
                widgetUI = self.builder.createWidget(def);
                widgetUI.appendTo(container);
            }
        }

        self.builder.onBuildComplete();
    };

    self.buildUI = function (step) {
        _(["Build UI",step]);
        try {
            container = $("#" + hal.cfg.view_config.elements.content);
            self.builder.updateModelFromStep();
            container.html('');
            if (step.content != null) {
                if (step.content.items != null) {
                    self.buildContent(step.content, container);
                }
            }
        } catch (e) {
            alert(e.message);
        }
    };
};
