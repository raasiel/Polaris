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
    self.templates = {
        text:  '<p class="halui text" id="${id}">${text}</p>',
        input: '<span class="hal input">${text}</span><input class="halui input" id="${id}"></input>',
        radio: '<div class="halui radio" id="${id}"><p>${text}</p></div>',
        option: '<div><input class="halui input" id="${id}" type="radio" value="${value}" name="group1">${text}</input></br><div>',
        action: '<input class="hal action" type="button" value="${text}" onclick="javascript:hal.${cmd}()"></input>'
    };

    
    self.createWidget = function (def) {
        _(["Create widget", def, container]);
        // Make sure UI element has an id.
        if (def.id == null) {
            self.maxId++;
            def.id = "control_" + self.maxId.toString();
        }

        var tmpl = self.templates[def._type];
        var tempContainer = null;
        var output = null;
        if (def.items != null) {
            tempContainer = $("<div/>");
            for (key in def.items) {
                childItem = def.items[key];
                childWidget = self.createWidget(childItem);
                childWidget.appendTo(tempContainer);
            }
        }
        if (def.content != null) {
            if (def.content.items != null){
                tempContainer = $("<div/>");
                for (key in def.content.items) {
                    childItem = def.content.items[key];
                    childWidget = self.createWidget(childItem);
                    childWidget.appendTo(tempContainer);
                }
            }
        }

        output = $.tmpl(tmpl, def);
        if (tempContainer != null) {
            _(["temp container", tempContainer]);
            tempContainer.attr("id","parent_" + def.id);
            tempContainer.appendTo(output);
        }
        
        return output;
    }
    /*
        fn = new function (childDef, childContainer) {
            return "<p>ChildDef</p>";
        }
        if (def.items != null) {
            for (key in def.items) {
                childItem = def.items[key];
                if (childItem.content != null) {
                    /*
                    for (childKey in childItem.content.items) {
                        smallItem = childItem.content.items[childKey];                       
                        smallItem['html'] = function () { return "AAA"; };
                        _(smallItem);
                    }
                    childItem.html = function () { return "AAA"; };
                    _(childItem);
                }
            }
        }*/
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
    };

    self.buildUI = function (step) {
        _(["Build UI",step]);
        try {
            container = $("#" + hal.cfg.view_config.elements.content);
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








